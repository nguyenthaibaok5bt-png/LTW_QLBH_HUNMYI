using System.Web;
using System.Web.Mvc;

namespace LTW_QLBH_HUNMYI
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
