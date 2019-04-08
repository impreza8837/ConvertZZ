using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ConvertZZ.FastReplace;

namespace ConvertZZ
{
    public class ChineseConverter
    {
        #region OS的轉換
        internal const int LOCALE_SYSTEM_DEFAULT = 0x0800;
        internal const int LCMAP_SIMPLIFIED_CHINESE = 0x02000000;
        internal const int LCMAP_TRADITIONAL_CHINESE = 0x04000000;

        /// <summary> 
        /// 使用OS的kernel.dll做為簡繁轉換工具，只要有裝OS就可以使用，不用額外引用dll，但只能做逐字轉換，無法進行詞意的轉換 
        /// <para>所以無法將電腦轉成計算機</para> 
        /// </summary> 
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int LCMapString(int Locale, int dwMapFlags, string lpSrcStr, int cchSrc, [Out] string lpDestStr, int cchDest);

        /// <summary> 
        /// 繁體轉簡體 
        /// </summary> 
        /// <param name="pSource">要轉換的繁體字：體</param> 
        /// <returns>轉換後的簡體字：體</returns> 
        public static string ToSimplified(string pSource)
        {
            String tTarget = new String(' ', pSource.Length);
            int tReturn = LCMapString(LOCALE_SYSTEM_DEFAULT, LCMAP_SIMPLIFIED_CHINESE, pSource, pSource.Length, tTarget, pSource.Length);
            return tTarget;
        }

        /// <summary> 
        /// 簡體轉繁體 
        /// </summary> 
        /// <param name="pSource">要轉換的繁體字：體</param> 
        /// <returns>轉換後的簡體字：體</returns> 
        public static string ToTraditional(string pSource)
        {
            String tTarget = new String(' ', pSource.Length);
            int tReturn = LCMapString(LOCALE_SYSTEM_DEFAULT, LCMAP_TRADITIONAL_CHINESE, pSource, pSource.Length, tTarget, pSource.Length);
            return tTarget;
        }

        #endregion OS的轉換

        private SortedDictionary<string, string> _dictionary, _dictionaryRevert;
        private bool _hasError;
        private StringBuilder _logs;
        FastReplace FR = new FastReplace(), FRRevert = new FastReplace();
        public ChineseConverter()
        {
            var cmp = new WordMappingComparer();
            _dictionary = new SortedDictionary<string, string>(cmp);
            _dictionaryRevert = new SortedDictionary<string, string>(cmp);
            _logs = new StringBuilder();
            _hasError = false;
        }
        public void ClearLogs()
        {
            _logs.Clear();
            _hasError = false;
        }

        public async Task LoadDatabase(string databasePath)
        {
            var db = new SQLiteAsyncConnection(databasePath);
            await db.CreateTableAsync<TranslateDictionary>();
            var list = await db.Table<TranslateDictionary>().OrderBy(x => x.SimplifiedChinese_Priority).ThenByDescending(x => x.SimplifiedChinese_Length).ToListAsync();
            list.ForEach(x => {
                if (!_dictionary.ContainsKey(x.SimplifiedChinese))
                    _dictionary.Add(x.SimplifiedChinese, x.TraditionalChinese);
            });
            list.Sort((x, y) => { int a = -x.TraditionalChinese_Priority.CompareTo(y.TraditionalChinese_Priority); if (a == 0) return -x.TraditionalChinese_Length.CompareTo(y.TraditionalChinese_Length); else return a; });
            list.ForEach(x => {
                if (!_dictionaryRevert.ContainsKey(x.TraditionalChinese))
                    _dictionaryRevert.Add(x.TraditionalChinese, x.SimplifiedChinese);
            });
        }

        public async void DictionaryToDatabase(string databasePath)
        {
            var db = new SQLiteAsyncConnection(databasePath);
            await db.CreateTableAsync<TranslateDictionary>();
            await db.CreateIndexAsync("TranslateDictionary", new string[] { "TraditionalChinese_Priority", "TraditionalChinese_Length" });
            await db.CreateIndexAsync("TranslateDictionary", new string[] { "SimplifiedChinese_Priority", "SimplifiedChinese_Length" });
            await db.RunInTransactionAsync((SQLiteConnection connection) => {
                foreach (var v in _dictionary)
                    connection.InsertOrReplace(new TranslateDictionary(v.Key, v.Value));
            });
            await db.CloseAsync();
        }


        public void ReloadFastReplaceDic()
        {
            FR = new FastReplace(_dictionary);
            FRRevert = new FastReplace(_dictionaryRevert);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="C2T">True:簡體轉繁體  False:繁體轉簡體</param>
        /// <returns></returns>
        public string Convert(string input, bool C2T)
        {
            //這個方法最快
            if (C2T)
                return FR.ReplaceAll(input);
            else
                return FRRevert.ReplaceAll(input);
            /* 第二快
            foreach (var temp in _dictionary)
            {
                input = input.Replace(temp.Key, temp.Value);
            }
            return input;*/
            /* 最慢
            StringBuilder sb = new StringBuilder(input);
            foreach(var temp in _dictionary)
            {
                sb.Replace(temp.Key, temp.Value);                    
            }
            return input;*/
        }

        public void DumpKeys()
        {
            foreach (var key in _dictionary.Keys)
            {
                Console.WriteLine(key);
            }
            foreach (var key in _dictionaryRevert.Keys)
            {
                Console.WriteLine(key);
            }
        }

        public bool HasError
        {
            get
            {
                return _hasError;
            }
        }

        public string Logs
        {
            get
            {
                return _logs.ToString();
            }
        }
    }
}
