using System;
using System.Collections.Generic;

namespace Core.Excel {
    // 自动生成信息，请勿手动修改
    // 日期: 2026/10/20 10:11:39

    [Serializable]
    public class cesConfig
    {
        public int Id { get; set; }
        public float Value1 { get; set; }
        public int MCount { get; set; }
        public List<int> Formalua { get; set; }
        public string Text { get; set; }

        public int GetKey() => Id;
    }

}
