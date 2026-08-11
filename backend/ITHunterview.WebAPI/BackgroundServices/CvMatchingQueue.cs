using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ITHunterview.WebAPI.BackgroundServices
{
    public class CvMatchingRequest
    {
        public Guid CvId { get; set; }
        public Guid UserId { get; set; }
        public bool IsHardcode { get; set; }
    }

    public interface ICvMatchingQueue
    {
        ValueTask QueueMatchRequestAsync(CvMatchingRequest request, CancellationToken cancellationToken = default);
        ValueTask<CvMatchingRequest> DequeueAsync(CancellationToken cancellationToken);
    }

    public class CvMatchingQueue : ICvMatchingQueue
    {
        private readonly Channel<CvMatchingRequest> _queue;

        public CvMatchingQueue()
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<CvMatchingRequest>(options);
        }

        public async ValueTask QueueMatchRequestAsync(CvMatchingRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await _queue.Writer.WriteAsync(request, cancellationToken);
        }

        public async ValueTask<CvMatchingRequest> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
