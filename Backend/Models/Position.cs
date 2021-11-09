// ReSharper disable once CheckNamespace
namespace Backend;

public partial class Position
{
    public bool IsWithinViewport(Viewport viewport)
    {
        // naive implementation, ignores edge cases
        if (viewport.SouthWest is null || viewport.NorthEast is null)
        {
            return false;
        }

        return Longitude >= viewport.SouthWest.Longitude &&
               Latitude >= viewport.SouthWest.Longitude &&
               Longitude <= viewport.NorthEast.Longitude &&
               Latitude <= viewport.NorthEast.Latitude;
    }
}