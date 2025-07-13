﻿using System;

namespace Avalux.OpenAi.Protocol.Schemas
{
    public class ApiEndpointSchema
    {
        public string Input { get; set; }
        public string Output { get; set; }
        public string Mode { get; set; } = "string";
        public string[] Prompts { get; set; } = Array.Empty<string>();
    }
}