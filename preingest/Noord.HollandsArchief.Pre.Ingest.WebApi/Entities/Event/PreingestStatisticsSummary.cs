﻿using System;

namespace Noord.HollandsArchief.Pre.Ingest.WebApi.Entities.Event
{
    public class PreingestStatisticsSummary
    {
        public Int32 Processed { get; set; }
        public Int32 Accepted { get; set; }
        public Int32 Rejected { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
