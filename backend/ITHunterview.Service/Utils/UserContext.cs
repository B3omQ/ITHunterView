using System;
using System.Threading;

namespace ITHunterview.Service.Utils
{
    public static class UserContext
    {
        private static readonly AsyncLocal<Guid?> _currentUserId = new AsyncLocal<Guid?>();

        public static Guid? CurrentUserId
        {
            get => _currentUserId.Value;
            set => _currentUserId.Value = value;
        }
    }
}
