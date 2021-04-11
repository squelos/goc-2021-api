using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnapZen.Models
{
    public class ImageInput
    {
        public User user { get; set; }

        public string sessionGuid { get; set; }

        public string image { get; set; }

        public string imageName { get; set; }
    }
}
