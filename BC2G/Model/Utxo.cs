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
        private const char _delimiter = ';';

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
        public string CreatedIn
        {
            set { _createdIn = value; }
            get { return _createdIn; }
        }
        private string _createdIn;

        public int CreatedInCount
        {
            set { _createdInCount = value; }
            get { return _createdInCount; }
        }
        private int _createdInCount;

        /// <summary>
        /// A list of comma-separated block heights where this txo
        /// is defined as input. If this list is empty, the tx is 
        /// unspent (utxo).
        /// </summary>
        public string ReferencedIn
        {
            set { _refdIn = value; }
            get { return _refdIn; }
        }
        private string _refdIn;

        public int ReferencedInCount
        {
            set { _refdInCount = value; }
            get { return _refdInCount; }
        }
        private int _refdInCount;

        // This constructor is required by EF.
        public Utxo(string id, string address, double value, string createdIn, string referencedIn = "")
        {
            Id = id;
            Address = address;
            Value = value;
            CreatedIn = createdIn;
            ReferencedIn = referencedIn;

            if (!string.IsNullOrEmpty(createdIn))
                CreatedInCount = 1;
            if (!string.IsNullOrEmpty(referencedIn))
                ReferencedInCount = 1;
        }

        public Utxo(string txid, int voutN, string address, double value, string createdIn, string referencedIn = "") :
            this(GetId(txid, voutN), address, value, createdIn, referencedIn)
        { }

        public static string GetId(string txid, int voutN)
        {
            return $"{voutN}-{txid}";
        }

        public void AddReferencedIn(string address)
        {
            UpdateRefs(ref _refdIn, ref _refdInCount, address);
        }

        public void AddCreatedIn(string address)
        {
            UpdateRefs(ref _createdIn, ref _createdInCount, address);
        }

        private static void UpdateRefs(ref string refs, ref int counts, string newRef)
        {
            if (string.IsNullOrEmpty(newRef))
                return;

            var existingRefs = refs.Split(_delimiter);
            if (existingRefs.Contains(newRef))
                return;

            counts++;
            if (existingRefs.Length > 0)
                refs += _delimiter;
            refs += newRef;
        }
    }
}
