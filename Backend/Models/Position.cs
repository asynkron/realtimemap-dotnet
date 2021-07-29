namespace Backend
{
    public partial class Position
    {
        public bool IsWithinViewport(Viewport viewport)
        {
            // naive implementation, ignores edge cases
            if (viewport.SouthWest is null || viewport.NorthEast is null)
            {
                return false;
            }

            return this.Longitude >= viewport.SouthWest.Longitude &&
                   this.Latitude >= viewport.SouthWest.Longitude &&
                   this.Longitude <= viewport.NorthEast.Longitude &&
                   this.Latitude <= viewport.NorthEast.Latitude;
        }
    }
}