//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Lab
{
    using System;
    using System.Collections.Generic;
    
    public partial class services_
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public services_()
        {
            this.s_in_o_ = new HashSet<s_in_o_>();
        }
    
        public int code { get; set; }
        public string service { get; set; }
        public Nullable<double> price { get; set; }
        public Nullable<int> due_date { get; set; }
        public Nullable<double> ot_deviation { get; set; }
        public Nullable<double> do_deviation { get; set; }
        public Nullable<int> metka { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<s_in_o_> s_in_o_ { get; set; }
    }
}
