using System.Collections.Generic;

namespace Ristlbat17.Disposition.Reporting
{
    public class ServantReportItemCompany
    {
        public string Company { get; set; }
        public int Ideal { get; set; }
        public int Stock { get; set; }
        public int Used { get; set; }
        public int Detached { get; set; }
        public int Available => Stock - Used - Detached;

        public List<ServantReportItemLocation> PerLocation { get; set; }

        protected bool Equals(ServantReportItemCompany other)
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

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ServantReportItemCompany) obj);
        }

        public override int GetHashCode()
        {
            return Company != null ? Company.GetHashCode() : 0;
        }
    }
}