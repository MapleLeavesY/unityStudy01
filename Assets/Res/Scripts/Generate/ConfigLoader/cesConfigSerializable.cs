using System.Collections.Generic;
using System.IO;

namespace Core.Excel
{
    // 自动生成信息，请勿手动修改
    // 日期: 2026/10/20 10:11:39

    public class cesConfigSerializable
    {
        public void Writer(BinaryWriter writer, cesConfig writerConfig)
        {
            writer.Write(writerConfig.Id);
            writer.Write(writerConfig.Value1);
            writer.Write(writerConfig.MCount);
            writer.Write(writerConfig.Formalua.Count);
            for (int i_3 = 0; i_3 < writerConfig.Formalua.Count; i_3++)
            {
                writer.Write(writerConfig.Formalua[i_3]);
            }
            writer.Write(writerConfig.Text);
        }


        public cesConfig Reader(BinaryReader reader)
        {
            var obj = new cesConfig();
            obj.Id = reader.ReadInt32();
            obj.Value1 = reader.ReadSingle();
            obj.MCount = reader.ReadInt32();
            var len_3 = reader.ReadInt32();
            obj.Formalua = new List<int>();
            for (int i_3 = 0; i_3 < len_3; i_3++)
            {
                obj.Formalua.Add(reader.ReadInt32());
            }
            obj.Text = reader.ReadString();

            return obj;
        }
    }
}
