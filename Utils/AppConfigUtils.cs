using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTransfer.Utils
{
    public class AppConfigUtils
    {
        public static void AddItem(string key, string value)
        {
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string appConfigPath = cfa.FilePath;
            FileAttributes attributes = File.GetAttributes(appConfigPath);
            // 移除只读属性
            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(appConfigPath, attributes & ~FileAttributes.ReadOnly);
            }

            //更新配置文件：
            if (cfa.AppSettings.Settings[key] != null)
            {
                //删除
                cfa.AppSettings.Settings.Remove(key);
            }

            //添加
            cfa.AppSettings.Settings.Add(key, value);
            //最后调用
            cfa.Save();
            //当前的配置文件更新成功
            ConfigurationManager.RefreshSection("appSettings");// 刷新命名节，在下次检索它时将从磁盘重新读取它。
        }
    }
}
