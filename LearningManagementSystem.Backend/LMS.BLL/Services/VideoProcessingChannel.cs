using System.Threading.Channels;
using System.Threading.Tasks;

namespace LMS.BLL.Services
{
    public class VideoProcessingChannel
    {
        private readonly Channel<string> _channel;

        public VideoProcessingChannel()
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _channel = Channel.CreateBounded<string>(options);
        }

        public async Task AddVideoAsync(string filePath)
        {
            await _channel.Writer.WriteAsync(filePath);
        }

        public IAsyncEnumerable<string> ReadAllAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
