﻿using System;
using System.IO;
using System.Xml.Serialization;

namespace Deck_Tracker_Copy_For_Study
{
    //https://msdn.microsoft.com/zh-cn/library/system.xml(v=vs.110).aspx
    //Todo 
    public class XmlManager<T>
    {
        public Type Type;

        public T Load(string path)
        {
            T instance;
            using (TextReader reader = new StreamReader(path))
            {
                XmlSerializer xml = new XmlSerializer(Type);
                instance = (T)xml.Deserialize(reader);
            }
            return instance;
        }

        public T LoadFromString(string xmlString)
        {
            T instance;
            using (TextReader reader = new StringReader(xmlString))
            {
                XmlSerializer xml = new XmlSerializer(Type);
                instance = (T)xml.Deserialize(reader);
            }
            return instance;
        }

        public void Save(string path, object obj)
        {
            using (TextWriter writer = new StreamWriter(path))
            {
                XmlSerializer xml = new XmlSerializer(Type);
                xml.Serialize(writer, obj);
            }
        }
    }
}
