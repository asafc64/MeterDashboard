using System.Collections.Immutable;
using MeterDashboard.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MeterDashboard.Web;



public static class WebExtensions
{
    public static void MapMeterDashboard(this IEndpointRouteBuilder builder, string baseRoute = "/meter-dashboard")
    {
        builder.MapPost(baseRoute + "/measurements", GetMeasurements);
    }

    // private static IEnumerable<MeasurementMetadataResponse> ListMeasurements(Storage storage, [FromBody] MeasurementRequest[] request)
    // {
    //     return storage.Measurements
    //         .Select(measurement => new MeasurementMetadataResponse
    //         {
    //             MeterName = measurement.Instrument.Meter.Name,
    //             InstrumentName = measurement.Instrument.Name,
    //             InstrumentUnit = measurement.Instrument.Unit,
    //         });
    // }

    
    private static IEnumerable<MeasurementResponse> GetMeasurements(Storage storage, [FromBody] MeasurementRequest? request)
    {
        var filteredMeasurements = storage.Measurements
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
                InstrumentType = instrument.GetType().GetNameWithoutGenericPostfix(),
                InstrumentUnit = instrument.Unit,
                Metrics = measurements
                    .Select(m =>
                    {
                        var dataPoints = m.GetDataPoints(null);
                        return new MeasurementResponse.Metric
                        {
                            Tags = m.Tags.ToDictionary(x => x.Key, x => x.Value),
                            Xs = dataPoints.Select(x => x.Timestamp).ToImmutableList(),
                            Ys = dataPoints.Select(x => x.Value).ToImmutableList(),
                        };
                    })
                    .ToImmutableList()
            };
        }
        
    }

    private static string GetNameWithoutGenericPostfix(this Type t)
    {
        var name = t.Name;
        var index = name.IndexOf('`');
        return index == -1 ? name : name.Substring(0, index);
    }
}