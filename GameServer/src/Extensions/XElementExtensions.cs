using System.Xml.Linq;

namespace FoolOnlineServer.Extensions
{
    public static class XElementExtensions
    {
        /// <summary>
        /// Finds element nested in XElement by local name
        /// </summary>
        /// <param name="body">XElement which to look</param>
        /// <param name="elementLocalName">Target name</param>
        /// <returns>Found xelement. Null if none</returns>
        public static XElement GetChildElement(this XElement body, string elementLocalName)
        {
            foreach (var element in body.Elements())
            {
                if (element.Name.LocalName == elementLocalName)
                {
                    return element;
                }
            }

            return null;
        }

    }

}
