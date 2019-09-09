﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using SampleBatch.Api.Models;
using SampleBatch.Contracts;
using SampleBatch.Contracts.Enums;

namespace SampleBatch.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BatchJobsController : ControllerBase
    {
        private readonly IRequestClient<SubmitBatch> _submitBatchClient;
        private readonly IPublishEndpoint _publishEndpoint;

        public BatchJobsController(IRequestClient<SubmitBatch> submitBatchClient, IPublishEndpoint publishEndpoint)
        {
            _submitBatchClient = submitBatchClient;
            _publishEndpoint = publishEndpoint;
        }

        // GET api/batchjobs
        [HttpGet(Name = "Get")]
        public IActionResult Get()
        {
            // Can query the DB within the API project, or move the query into a consumer, and use MT Req/Response
            return Ok();
        }

        // GET api/batchjobs/5
        [HttpGet("{id}", Name = "GetById")]
        public IActionResult Get(int id)
        {
            // Can query the DB within the API project, or move the query into a consumer, and use MT Req/Response
            return Ok();
        }

        // POST api/batchjobs/create
        [HttpPost("create", Name = "Create")]
        public async Task<ActionResult<Guid>> Post(int jobCount = 100, int activeThreshold = 10, int? delayInSeconds = null)
        {
            var id = NewId.NextGuid();
            var orderIds = new List<Guid>();
            for (int i = 0; i < jobCount; i++)
            {
                orderIds.Add(Guid.NewGuid());
            }

            var (accepted, rejected) = await _submitBatchClient.GetResponse<BatchSubmitted, BatchRejected>(new CreateBatch
            {
                BatchId = id,
                Timestamp = DateTime.UtcNow,
                Action = BatchAction.CancelOrders,
                OrderIds = orderIds.ToArray(),
                ActiveThreshold = activeThreshold,
                DelayInSeconds = delayInSeconds
            });

            if (accepted.IsCompleted)
            {
                var result = await accepted;

                return Accepted(id);
            }

            var response = await rejected;

            return BadRequest(response.Message.Reason);
        }

        [HttpPut("{id}/cancel", Name = "Cancel")]
        public async Task<ActionResult<Guid>> Cancel(Guid id)
        {
            await _publishEndpoint.Publish<CancelBatch>(new { BatchId = id, Timestamp = DateTime.UtcNow });

            return Accepted();
        }
    }
}
