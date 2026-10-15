using System;
using System.Collections.Generic;
using System.Text;

namespace Contracts
{
    public class AcutionUpdated
    {
        public required string Id { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public string? Description { get; set; }
        public int? Year { get; set; }
        public string? Color { get; set; }
        public int? Mileage { get; set; }
    }
}
