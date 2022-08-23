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

        /// <summary>
        /// A list of comma-separated block heights where this txo 
        /// is defined as output.
        /// </summary>
        [Required]
        public string CreatedIn { set; get; }
        public int CreatedInCount { set; get; }

        /// <summary>
        /// A list of comma-separated block heights where this txo
        /// is defined as input. If this list is empty, the tx is 
        /// unspent (utxo).
        /// </summary>
        public string ReferencedIn { set; get; }
        public int ReferencedInCount { set; get; }

        // This constructor is required by EF.
        public Utxo(string id, string address, double value, string createdIn, string referencedIn = "")
        {
            Id = id;
            Address = address;
            Value = value;
            CreatedIn = createdIn;
            ReferencedIn = referencedIn;
        }

        public Utxo(string txid, int voutN, string address, double value, string createdIn, string referencedIn = "") :
            this(GetId(txid, voutN), address, value, createdIn, referencedIn)
        { }

        public static string GetId(string txid, int voutN)
        {
            return $"{voutN}-{txid}";
        }
    }
}
