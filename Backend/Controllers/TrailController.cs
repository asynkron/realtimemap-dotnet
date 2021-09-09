using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.DTO;
using Microsoft.AspNetCore.Mvc;
using Proto.Cluster;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/trail")]
    public class TrailController : ControllerBase
    {
        private readonly Cluster _cluster;

        public TrailController(Cluster cluster)
        {
            _cluster = cluster;
        }

        [HttpGet("{assetId}")]
        public async Task<ActionResult<PositionsDto>> Get(string assetId)
        {
            var positionsHistory = await _cluster
                .GetVehicleActor(assetId)
                .GetPositionsHistory(new GetPositionsHistoryRequest(), CancellationToken.None);
            
            var positions = positionsHistory.Positions
                .Select(PositionDto.MapFrom)
                .ToArray();

            var result = new PositionsDto
            {
                Positions = positions
            };

            return Ok(result);
        }
    }
}