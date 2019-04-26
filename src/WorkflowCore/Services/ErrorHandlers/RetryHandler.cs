using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Services.ErrorHandlers
{
    public class RetryHandler : IWorkflowErrorHandler
    {
        private readonly IDateTimeProvider _datetimeProvider;
        private readonly WorkflowOptions _options;
        private readonly ILifeCycleEventPublisher _eventPublisher;

        public WorkflowErrorHandling Type => WorkflowErrorHandling.Retry;

        public RetryHandler(IDateTimeProvider datetimeProvider, WorkflowOptions options, ILifeCycleEventPublisher eventPublisher)
        {
            _datetimeProvider = datetimeProvider;
            _options = options;
            _eventPublisher = eventPublisher;
        }

        public void Handle(WorkflowInstance workflow, WorkflowDefinition def, ExecutionPointer pointer, WorkflowStep step, Exception exception, Queue<ExecutionPointer> bubbleUpQueue)
        {
            pointer.RetryCount++;
            pointer.SleepUntil = _datetimeProvider.Now.ToUniversalTime().Add(step.RetryInterval ?? def.DefaultErrorRetryInterval ?? _options.ErrorRetryInterval);
            if (_options.MaxRetries == 0 || pointer.RetryCount <= _options.MaxRetries)
                step.PrimeForRetry(pointer);
            else
            {
                workflow.Status = WorkflowStatus.Terminated;
                _eventPublisher.PublishNotification(new WorkflowTerminated()
                {
                    EventTimeUtc = _datetimeProvider.Now,
                    Reference = workflow.Reference,
                    WorkflowInstanceId = workflow.Id,
                    WorkflowDefinitionId = workflow.WorkflowDefinitionId,
                    Version = workflow.Version
                });
            }

        }
    }
}
