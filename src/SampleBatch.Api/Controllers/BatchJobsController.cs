using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using SampleBatch.Contracts;
using SampleBatch.Contracts.Enums;


namespace SampleBatch.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BatchJobsController :
        ControllerBase
    {
        readonly IRequestClient<SubmitBatch> _submitBatchClient;
        readonly IRequestClient<BatchStatusRequested> _batchStatusClient;
        readonly IPublishEndpoint _publishEndpoint;
        private readonly IDistributedCache _cache;

        public BatchJobsController(
            IRequestClient<SubmitBatch> submitBatchClient,
            IRequestClient<BatchStatusRequested> batchStatusClient,
            IPublishEndpoint publishEndpoint,
            IDistributedCache cache)
        {
            _submitBatchClient = submitBatchClient;
            _batchStatusClient = batchStatusClient;
            _publishEndpoint = publishEndpoint;
            _cache = cache;
        }

        // GET api/batchjobs
        [HttpGet(Name = "Get")]
        public async Task<IActionResult> Get()
        {
            var CachedTimeUTC = "Cached Time Expired";
            var encodedCachedTimeUTC = await _cache.GetAsync("cachedTimeUTC");

            if (encodedCachedTimeUTC != null)
            {
                CachedTimeUTC = Encoding.UTF8.GetString(encodedCachedTimeUTC);
            }
            // Can query the DB within the API project, or move the query into a consumer, and use MT Req/Response
            return Ok();
        }

        // GET api/batchjobs/5
        [HttpGet("{id}", Name = "GetById")]
        public async Task<ActionResult<BatchStatus>> Get(Guid id)
        {
            var (status, notFound) = await _batchStatusClient.GetResponse<BatchStatus, BatchNotFound>(new
            {
                BatchId = id,
                InVar.Timestamp,
            });

            if (notFound.IsCompletedSuccessfully)
            {
                await notFound;

                return NotFound(new
                {
                    BatchId = id
                });
            }

            Response<BatchStatus> response = await status;

            return Ok(response.Message);
        }

        // POST api/batchjobs/create
        [HttpPost("create", Name = "Create")]
        public async Task<ActionResult<Guid>> Post(int jobCount = 100, int activeThreshold = 10, int? delayInSeconds = null)
        {
            var id = NewId.NextGuid();
            var orderIds = new List<Guid>();
            for (int i = 0; i < jobCount; i++)
            {
                orderIds.Add(NewId.NextGuid());
            }

            var (accepted, rejected) = await _submitBatchClient.GetResponse<BatchSubmitted, BatchRejected>(new
            {
                BatchId = id,
                InVar.Timestamp,
                Action = BatchActionEnum.CancelOrders,
                OrderIds = orderIds.ToArray(),
                ActiveThreshold = activeThreshold,
                DelayInSeconds = delayInSeconds
            });

            if (accepted.IsCompleted)
            {
                await accepted;

                return Accepted(id);
            }

            var response = await rejected;

            return BadRequest(response.Message.Reason);
        }

        [HttpPut("{id}/cancel", Name = "Cancel")]
        public async Task<ActionResult<Guid>> Cancel(Guid id)
        {
            await _publishEndpoint.Publish<CancelBatch>(new
            {
                BatchId = id,
                InVar.Timestamp
            });

            return Accepted();
        }
    }
}