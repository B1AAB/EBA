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
    public class Utxo
    {
        [Required]
        public string Id { set; get; } = string.Empty;
        [Required]
        public string Address { set; get; } = string.Empty;
        [Required]
        public double Value { set; get; }

        // This constructor is required by EF.
        public Utxo(string id, string address, double value)
        {
            Id = id;
            Address = address;
            Value = value;
        }

        public Utxo(string txid, int voutN, string address, double value) : 
            this(GetId(txid, voutN), address, value) 
        { }

        public static string GetId(string txid, int voutN)
        {
            return $"{voutN}-{txid}";
        }
    }
}
