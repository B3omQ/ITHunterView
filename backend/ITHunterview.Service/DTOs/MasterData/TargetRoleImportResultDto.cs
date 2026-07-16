using System;
using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.MasterData
{
    public class TargetRoleImportResultDto
    {
        public int ImportedCount { get; set; }
        public int UpdatedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
