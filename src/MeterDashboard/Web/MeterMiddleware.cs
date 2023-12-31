﻿using System.Collections.Immutable;
using System.Text;
using MeterDashboard.Services;
using MeterDashboard.Web.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MeterDashboard.Web;

class MeterMiddleware : IMiddleware
{
    private const string PathBase = "/meter-dashboard";
    private const string StaticFileNamespace = "MeterDashboard.Web.StaticFiles";
    private readonly Storage _storage;
    private readonly string[] _staticFiles;

    public MeterMiddleware(Storage storage)
    {
        _storage = storage;
        _staticFiles = GetType().Assembly.GetManifestResourceNames();
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Path.StartsWithSegments(PathBase))
        {
            await next(context);
            return;
        }

        if (await ProvideStaticFile(context))
            return;
        
        if (await ProvideApiCall(context))
            return;
        
        context.Response.StatusCode = 404;
    }

    private async Task<bool> ProvideStaticFile(HttpContext context)
    {
        if (context.Request.Method != "GET")
            return false;

        var fileRelativePath = GetRelativePath(context).TrimStart('/');
        if (fileRelativePath == string.Empty)
        {
            context.Response.Redirect(PathBase + "/index.html");
            return true;
        }
        
        var file = StaticFileNamespace + "." + fileRelativePath.Replace("/", ".");
        if (!_staticFiles.Contains(file))
            return false;
        
        if (file.EndsWith("index.js"))
        {
            var command = $"window.apiPort = {context.Connection.LocalPort};\n";
            await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(command));
        }
        
        await using var stream = GetType().Assembly.GetManifestResourceStream(file);
        await stream.CopyToAsync(context.Response.Body);
        return true;
    }

    private async Task<bool> ProvideApiCall(HttpContext context)
    {
        var httpMethod = context.Request.Method;
        var relativePath = GetRelativePath(context);
        
        if (httpMethod == "POST" && relativePath == "/api/measurements")
        {
            var request = await context.Request.ReadFromJsonAsync<MeasurementRequest>();
            var results = GetMeasurements(request).ToList();
            try
            {
                await context.Response.WriteAsJsonAsync(results);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
            return true;
        }

        return false;
    }

    private IEnumerable<MeasurementResponse> GetMeasurements(MeasurementRequest? request)
    {
        var filteredMeasurements = _storage.Measurements
            .Where(m => request?.FilterMeterNames == null ||
                        !request.FilterMeterNames.Any() ||
                        request.FilterMeterNames.Contains(m.Instrument.Meter.Name))
            .GroupBy(x => x.Instrument);
        
        foreach (var group in filteredMeasurements)
        {
            var instrument = group.Key;
            var measurements = group.ToArray();

            // var dataPoints = measurements[0].GetMergedDataPoints(measurements[1..], request?.LastDate);
            yield return new MeasurementResponse
            {
                MeterName = instrument.Meter.Name, 
                InstrumentName = instrument.Name, 
                InstrumentType = GetNameWithoutGenericPostfix(instrument.GetType()),
                InstrumentUnit = instrument.Unit,
                Groups = measurements
                    .SelectMany(m =>
                    {
                        var dataPoints = m.GetDataPoints(null);
                        var byWindow = dataPoints.ToLookup(x => x.Window);
                        return byWindow
                            .Select(g => new MeasurementResponse.Group
                            {
                                Tags = m.Tags.ToDictionary(x => x.Key, x => x.Value),
                                Window = g.Key.ToString(),
                                Xs = g.Select(x => x.Timestamp).ToImmutableList(),
                                Ys = g.Select(x => x.Value).ToImmutableList(),
                            });
                    })
                    .ToImmutableList()
            };
        }
        
    }

    private static string GetNameWithoutGenericPostfix(Type t)
    {
        var name = t.Name;
        var index = name.IndexOf('`');
        return index == -1 ? name : name.Substring(0, index);
    }

    private static string GetRelativePath(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        return path[PathBase.Length..];
    }
}