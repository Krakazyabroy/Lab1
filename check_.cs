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
    
    public partial class check_
    {
        public int id { get; set; }
        public Nullable<double> sum { get; set; }
        public Nullable<int> id_bux { get; set; }
        public Nullable<System.DateTime> date_ot { get; set; }
        public Nullable<System.DateTime> date_do { get; set; }
        public Nullable<int> id_comp { get; set; }
    
        public virtual insurance_company_ insurance_company_ { get; set; }
        public virtual users_ users_ { get; set; }
    }
}
