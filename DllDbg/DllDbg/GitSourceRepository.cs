using System.IO;
using System.Text.RegularExpressions;

namespace DllDbg
{
    public class GitSourceRepository : ISourceRepository, IInterrogator
    {
        private readonly IResponseParser responseParser;
        private readonly DirectoryInfo checkoutLocation;
        private string revision;

        public GitSourceRepository(IResponseParser responseParser, DirectoryInfo checkoutLocation)
        {
            this.responseParser = responseParser;
            this.checkoutLocation = checkoutLocation;
        }

        public void Verify(string revision, IMessageSubscriber subscriber)
        {
            this.revision = revision;
            var revisionHashLength = "280bb4850c149a0a33412093cba5d22b4d10bff4".Length;
            if (revision.Length == revisionHashLength && Regex.Match(revision, "[0-9a-fA-F]*").Success)
            {
                subscriber.Published(string.Format("Revision number is a valid Git hash."));
                subscriber.Ask("Pull this revision? (Y/N)", this);
            }
        }

        public void AnsweredWith(string response, IMessageSubscriber subscriber)
        {
            if (responseParser.Yes(response))
                subscriber.Published(string.Format("Pulling source corresponding to {0}", revision));
            else subscriber.Published(string.Format("Skipping revision {0}", revision));
        }
    }
}