namespace Backend
{
    public partial class Position
    {
        public bool IsWithinViewport(Viewport viewport)
        {
            // naive implementation, ignores edge cases
            return this.Longitude >= viewport.Lng1 &&
                   this.Longitude <= viewport.Lng2 &&
                   this.Latitude >= viewport.Lat1 &&
                   this.Latitude <= viewport.Lat2;
        }
    }
}