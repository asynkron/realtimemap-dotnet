using System.Linq;
using System.Threading.Tasks;
using Backend;
using Microsoft.AspNetCore.Mvc;
using Proxy.DTO;
using Proxy.Hubs;

namespace Proxy.Controllers
{
    [ApiController]
    [Route("api/trail")]
    public class TrailController : ControllerBase
    {
        private readonly MapBackend.MapBackendClient _client;

        public TrailController(MapBackend.MapBackendClient client)
        {
            _client = client;
        }

        [HttpGet("{assetId}")]
        public async Task<ActionResult<PositionsDto>> Get(string assetId)
        {
            var trail = await _client.GetTrailAsync(new GetTrailRequest { AssetId = assetId });

            var positions = trail.PositionBatch.Positions.Select(PositionDto.MapFrom).ToArray();

            var result = new PositionsDto
            {
                Positions = positions
            };

            return Ok(result);
        }
    }
}