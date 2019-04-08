using SQLite;

namespace ConvertZZ
{
    public class TranslateDictionary
    {
        public TranslateDictionary()
        {
            SimplifiedChinese = "";
            SimplifiedChinese_Length = 0;
            TraditionalChinese = "";
            TraditionalChinese_Length = 0;
            SimplifiedChinese_Priority = 1;
            TraditionalChinese_Priority = 1;
        }
        public TranslateDictionary(string SimplifiedChinese, string TraditionalChinese, int SimplifiedChinese_Priority = 1, int TraditionalChinese_Priority = 1)
        {
            this.SimplifiedChinese = SimplifiedChinese;
            this.SimplifiedChinese_Length = SimplifiedChinese.Length;
            this.TraditionalChinese = TraditionalChinese;
            this.TraditionalChinese_Length = TraditionalChinese.Length;
            this.SimplifiedChinese_Priority = SimplifiedChinese_Priority;
            this.TraditionalChinese_Priority = TraditionalChinese_Priority;
        }

        [PrimaryKey, Unique]
        public string SimplifiedChinese { get; set; }
        public int SimplifiedChinese_Length { get; set; }

        public string TraditionalChinese { get; set; }
        public int TraditionalChinese_Length { get; set; }

        public int SimplifiedChinese_Priority { get; set; }
        public int TraditionalChinese_Priority { get; set; }

        //預留欄位
        public string Reserved1 { get; set; }
        public string Reserved2 { get; set; }
        public string Reserved3 { get; set; }
        public string Reserved4 { get; set; }
        public string Reserved5 { get; set; }
        public string Reserved6 { get; set; }
        public string Reserved7 { get; set; }
        public string Reserved8 { get; set; }
        public string Reserved9 { get; set; }
        public string Reserved10 { get; set; }
    }
}
