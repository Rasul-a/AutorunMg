using System;
using System.Text.RegularExpressions;

namespace AutorunMg
{
    public class PathParse
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Params { get; set; }
        public PathParse(string filePath)
        {

            Regex regex = new Regex(@"[^\\:*?""<>|+]+\.[^\\\s]+($|\s)");// имя фала с расширением
            string s = filePath.Replace(@"""", "");
            string fileName = regex.Match(s).ToString();

            if (String.IsNullOrWhiteSpace(fileName))
                throw new Exception("Неверный путь");
            //разбиваем строку на подстроки(путь и параметры) 
            string[] str = s.Split(new string[] { fileName }, StringSplitOptions.None);
            Name = fileName;
            Path = str[0];
            Params = str[1];
        }
    }
}
