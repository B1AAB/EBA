using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.Model
{
    // TODO: can this be merged with the Output type?!

    [Table("Utxo")]
    internal class Utxo
    {
        [Required]
        public string Id { set; get; } = string.Empty;
        [Required]
        public string Address { set; get; } = string.Empty;
        [Required]
        public double Value { set; get; }
    }
}
