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
    
    public partial class s_in_o_
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public s_in_o_()
        {
            this.data_analyzer_ = new HashSet<data_analyzer_>();
        }
    
        public int id { get; set; }
        public Nullable<int> id_order { get; set; }
        public Nullable<int> code { get; set; }
        public Nullable<int> id_status { get; set; }
        public string BarCode { get; set; }
        public string result { get; set; }
        public int progress { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<data_analyzer_> data_analyzer_ { get; set; }
        public virtual order_ order_ { get; set; }
        public virtual services_ services_ { get; set; }
        public virtual status_ status_ { get; set; }
    }
}
