//------------------------------------------------------------------------------
// Расширение для класса Leases
// Добавляет свойства которые не были автоматически сгенерированы Entity Framework
//------------------------------------------------------------------------------

namespace WpfApp1
{
    using System;
    
    public partial class Leases
    {
        /// <summary>
        /// Причина расторжения/завершения договора
        /// </summary>
        public string TerminationReason { get; set; }
        
        /// <summary>
        /// Флаг архивации договора
        /// </summary>
        public bool IsArchived { get; set; }
    }
}
