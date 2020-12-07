using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace Noord.HollandsArchief.Pre.Ingest.Utilities
{
    /// <summary>
    /// A helper class to serialize object from/to XML
    /// </summary>
    public static class SerializerHelper
    {
        /// <summary>
        /// Serializes the object to string.
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns>In string format</returns>
        public static string SerializeObjectToString(Object objectToSerialize)
        {
            string result = null;

            if (objectToSerialize == null)
                return result;
            
            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(objectToSerialize.GetType());
                    serializer.Serialize(ms, objectToSerialize);
                    ms.Position = 0;
                    StreamReader rdr = new StreamReader(ms);
                    result = rdr.ReadToEnd();
                }
                catch (SerializationException)
                {
                    result = null;
                }
                finally { }
            }

            return result;
        }

        /// <summary>
        /// Serializes the object to XML element.
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns>XmlElement</returns>
        public static XmlElement SerializeObjectToXmlElement(Object objectToSerialize)
        {
            if (objectToSerialize == null)
                return null;

            XmlDocument doc = new XmlDocument();

            using (XmlWriter writer = doc.CreateNavigator().AppendChild())            
                new XmlSerializer(objectToSerialize.GetType()).Serialize(writer, objectToSerialize);
            
            return doc.DocumentElement;
        }
               
        /// <summary>
        /// Serializes an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializableObject"></param>
        /// <param name="fileName"></param>
        public static void SerializeObjectToXmlFile<T>(T serializableObject, string fileName)
        {
            if (serializableObject == null) { return; }

            XmlDocument xmlDocument = new XmlDocument();
            XmlSerializer serializer = new XmlSerializer(typeof(T));
                       
            string tempFileName = String.Format(@"{0}", System.IO.Path.GetTempFileName());

            using (MemoryStream stream = new MemoryStream())
            {
                serializer.Serialize(stream, serializableObject);
                stream.Position = 0;
                xmlDocument.Load(stream);
                xmlDocument = RemoveXmlns(xmlDocument);

                XmlDeclaration xmldecl;
                xmldecl = xmlDocument.CreateXmlDeclaration("1.0", null, null);
                xmldecl.Encoding = "UTF-8";

                XmlElement root = xmlDocument.DocumentElement;
                xmlDocument.InsertBefore(xmldecl, root);

                xmlDocument.Save(tempFileName);
                stream.Close();
            }
            System.IO.File.Move(tempFileName, fileName);
        }

        public static XmlDocument RemoveXmlns(XmlDocument doc)
        {
            XDocument d;
            using (var nodeReader = new XmlNodeReader(doc))
                d = XDocument.Load(nodeReader);

            d.Root.Descendants().Attributes().Where(x => x.IsNamespaceDeclaration).Remove();

            foreach (var elem in d.Descendants())
                elem.Name = elem.Name.LocalName;

            var xmlDocument = new XmlDocument();
            using (var xmlReader = d.CreateReader())
                xmlDocument.Load(xmlReader);

            return xmlDocument;
        }        

        /// <summary>
        /// Writes the given object instance to a binary file.
        /// <para>Object type (and all child types) must be decorated with the [Serializable] attribute.</para>
        /// <para>To prevent a variable from being serialized, decorate it with the [NonSerialized] attribute; cannot be applied to properties.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the XML file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the XML file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void SerializeObjectToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }        
    }
}
