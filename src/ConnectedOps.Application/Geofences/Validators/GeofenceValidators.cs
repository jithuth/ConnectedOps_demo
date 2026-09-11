using System.Text.Json;
using ConnectedOps.Domain.Geofences;
using FluentValidation;

namespace ConnectedOps.Application.Geofences.Validators;

public sealed class CreateGeofenceRequestValidator : AbstractValidator<CreateGeofenceRequest>
{
    public CreateGeofenceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Geofence name is required.")
            .MaximumLength(150).WithMessage("Geofence name cannot exceed 150 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Geofence code is required.")
            .MaximumLength(50).WithMessage("Geofence code cannot exceed 50 characters.");

        RuleFor(x => x.ColorHex)
            .NotEmpty().WithMessage("Color is required.")
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid 6-character hex code (e.g. #3B82F6).");

        When(x => x.GeofenceType == GeofenceType.Circle, () =>
        {
            RuleFor(x => x.CenterLatitude)
                .NotNull().WithMessage("Center latitude is required for circle geofences.")
                .InclusiveBetween(-90.0, 90.0).WithMessage("Latitude must be between -90 and +90 degrees.");

            RuleFor(x => x.CenterLongitude)
                .NotNull().WithMessage("Center longitude is required for circle geofences.")
                .InclusiveBetween(-180.0, 180.0).WithMessage("Longitude must be between -180 and +180 degrees.");

            RuleFor(x => x.RadiusMeters)
                .NotNull().WithMessage("Radius in meters is required for circle geofences.")
                .GreaterThan(0).WithMessage("Radius must be greater than 0 meters.")
                .LessThanOrEqualTo(500000).WithMessage("Radius cannot exceed 500 km.");
        });

        When(x => x.GeofenceType == GeofenceType.Polygon, () =>
        {
            RuleFor(x => x.PolygonGeoJson)
                .NotEmpty().WithMessage("Polygon GeoJSON or coordinate array is required for polygon geofences.")
                .Must(BeValidPolygonGeoJson).WithMessage("Polygon geometry must be valid GeoJSON with at least 3 distinct vertices.");
        });
    }

    private static bool BeValidPolygonGeoJson(string? geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(geoJson);
            var root = doc.RootElement;

            // Support either direct array of coordinates [[lon,lat],[lon,lat],...] or GeoJSON Polygon object { "type": "Polygon", "coordinates": [[[lon,lat],...]] }
            JsonElement coordinatesArray;

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("coordinates", out var coordsProp))
            {
                if (coordsProp.ValueKind != JsonValueKind.Array || coordsProp.GetArrayLength() == 0)
                    return false;

                coordinatesArray = coordsProp[0]; // First linear ring
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                coordinatesArray = root;
            }
            else
            {
                return false;
            }

            int count = coordinatesArray.GetArrayLength();
            return count >= 3;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class UpdateGeofenceRequestValidator : AbstractValidator<UpdateGeofenceRequest>
{
    public UpdateGeofenceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Geofence name is required.")
            .MaximumLength(150).WithMessage("Geofence name cannot exceed 150 characters.");

        RuleFor(x => x.ColorHex)
            .NotEmpty().WithMessage("Color is required.")
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid 6-character hex code (e.g. #3B82F6).");

        When(x => x.GeofenceType == GeofenceType.Circle, () =>
        {
            RuleFor(x => x.CenterLatitude)
                .NotNull().WithMessage("Center latitude is required for circle geofences.")
                .InclusiveBetween(-90.0, 90.0).WithMessage("Latitude must be between -90 and +90 degrees.");

            RuleFor(x => x.CenterLongitude)
                .NotNull().WithMessage("Center longitude is required for circle geofences.")
                .InclusiveBetween(-180.0, 180.0).WithMessage("Longitude must be between -180 and +180 degrees.");

            RuleFor(x => x.RadiusMeters)
                .NotNull().WithMessage("Radius in meters is required for circle geofences.")
                .GreaterThan(0).WithMessage("Radius must be greater than 0 meters.")
                .LessThanOrEqualTo(500000).WithMessage("Radius cannot exceed 500 km.");
        });

        When(x => x.GeofenceType == GeofenceType.Polygon, () =>
        {
            RuleFor(x => x.PolygonGeoJson)
                .NotEmpty().WithMessage("Polygon GeoJSON or coordinate array is required for polygon geofences.");
        });
    }
}
