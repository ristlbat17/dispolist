using System.Collections.Generic;

namespace Ristlbat17.Disposition.Reporting
{
    public class MaterialReportItemCompany
    {
        public string Company { get; set; }
        public int Stock { get; set; }
        public int Used { get; set; }
        public int Damaged { get; set; }
        public int Available => Stock - Used - Damaged;

        public List<MaterialReportItemLocation> PerLocation { get; set; }

        protected bool Equals(MaterialReportItemCompany other)
        {
            return string.Equals(Company, other.Company);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((MaterialReportItemCompany) obj);
        }

        public override int GetHashCode()
        {
            return (Company != null ? Company.GetHashCode() : 0);
        }
    }
}