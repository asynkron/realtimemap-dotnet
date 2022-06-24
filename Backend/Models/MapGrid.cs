namespace Backend.Models;

// to demonstrate how to scale pub-sub in a cluster, we divide the map to a grid 
// each cell in the grid has it's own pub-sub topic
// positions published from vehicles go to the topic matching the positions' cell
// user subscribes to all the cells that are inside the viewport
public class MapGrid
{
    private readonly double _gridGranularity;
    private readonly double _swLat;
    private readonly double _swLon;
    private readonly double _neLat;
    private readonly double _neLon;

    private string[,] _topics;

    public MapGrid(double gridGranularity, double swLat, double swLon, double neLat, double neLon) {
        _gridGranularity = gridGranularity;
        _swLat = swLat;
        _swLon = swLon;
        _neLat = neLat;
        _neLon = neLon;
        
        // snap to min and max position to nearest _gridGranularityInDegrees
        _swLat = Math.Round(_swLat / _gridGranularity) * _gridGranularity;
        _swLon = Math.Round(_swLon / _gridGranularity) * _gridGranularity;
        _neLat = Math.Round(_neLat / _gridGranularity) * _gridGranularity;
        _neLon = Math.Round(_neLon / _gridGranularity) * _gridGranularity;
        
        _topics = new string[
            (int)Math.Ceiling((_neLat - _swLat) / _gridGranularity) - 1, 
            (int)Math.Ceiling((_neLon - _swLon) / _gridGranularity) - 1];
        
        // pre-generate topic names
        for (int i = 0; i < _topics.GetLength(0); i++) {
            for (int j = 0; j < _topics.GetLength(1); j++) {
                _topics[i, j] = $"cell-{_swLat + i * _gridGranularity:F2}/{_swLon + j * _gridGranularity:F2}";
            }
        }
        
    }

    public string? TopicFromPosition(Position position)
    {
        if(position.Latitude < _swLat || position.Latitude >= _neLat || position.Longitude < _swLon || position.Longitude >= _neLon)
            return null;
        
        var swLatDelta = position.Latitude - _swLat;
        var swLonDelta = position.Longitude - _swLon;
        var swLatIndex = (int)Math.Floor(swLatDelta / _gridGranularity);
        var swLonIndex = (int)Math.Floor(swLonDelta / _gridGranularity);
        
        if(swLatIndex < 0 || swLatIndex >= _topics.GetLength(0) || swLonIndex < 0 || swLonIndex >= _topics.GetLength(1))
            return null;
        
        return _topics[swLatIndex, swLonIndex];
    }

    public string[] TopicsFromViewport(Viewport viewport)
    {
        var swLatDelta = viewport.SouthWest.Latitude - _swLat;
        var swLonDelta = viewport.SouthWest.Longitude - _swLon;
        var swLatIndex = Math.Max((int) Math.Floor(swLatDelta / _gridGranularity), 0);
        var swLonIndex = Math.Max((int) Math.Floor(swLonDelta / _gridGranularity), 0);

        var neLatDelta = viewport.NorthEast.Latitude - _swLat;
        var neLonDelta = viewport.NorthEast.Longitude - _swLon;
        var neLatIndex = Math.Min((int) Math.Floor(neLatDelta / _gridGranularity), _topics.GetLength(0) - 1);
        var neLonIndex = Math.Min((int) Math.Floor(neLonDelta / _gridGranularity), _topics.GetLength(1) - 1);

        var topics = new List<string>();
        for (var i = swLatIndex; i <= neLatIndex; i++)
            for (var j = swLonIndex; j <= neLonIndex; j++)
                topics.Add(_topics[i, j]);

        return topics.ToArray();
    }
}