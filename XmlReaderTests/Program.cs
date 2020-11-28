using System;
using System.IO;
using System.Xml;

namespace XmlReaderTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var xml = 
                "<Data>\r\n" +
                "   <Person>\r\n" +
                "       <Name>Bob</Name>\r\n" +
                "   </Person>\r\n" +
				"   <Person>\r\n" +
				"       <Name>Tracy</Name>\r\n" +
				"   </Person>\r\n" +
				"</Data>\r\n";

            var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { IgnoreWhitespace = true });

			reader.Read(); // Data

			reader.Read(); // Person
			Console.WriteLine("<{0}>", reader.Name);
			Console.Write("\t");
			reader.Read(); // Name
			Console.Write("<{0}>", reader.Name);
			reader.Read();
			Console.Write("{0}", reader.ReadContentAsString());
			//Console.Write("{0}", reader.ReadElementContentAsString());

			 while (reader.Read())
			{
				if (reader.IsStartElement())
				{
					if (reader.IsEmptyElement)
					{
						Console.WriteLine("<{0}/>", reader.Name);
					}
					else
					{
						Console.Write("<{0}> ", reader.Name);
						reader.Read(); // Read the start tag.
						if (reader.IsStartElement())  // Handle nested elements.
							Console.Write("\r\n<{0}>", reader.Name);
						Console.WriteLine(reader.ReadString());  //Read the text content of the element.
					}
				}
			}

			void ReadPerson(XmlReader reader)
            {

            }

			void ReadName(XmlReader reader)
            {

            }

			while (reader.Read())
			{
				if (reader.IsStartElement())
				{
					if (reader.IsEmptyElement)
					{
						Console.WriteLine("<{0}/>", reader.Name);
					}
					else
					{
						Console.Write("<{0}> ", reader.Name);
						reader.Read(); // Read the start tag.
						if (reader.IsStartElement())  // Handle nested elements.
							Console.Write("\r\n<{0}>", reader.Name);
						Console.WriteLine(reader.ReadString());  //Read the text content of the element.
					}
				}
			}

			Console.WriteLine(reader.NodeType);

            reader.MoveToElement();
            Console.WriteLine(reader.NodeType);

            //Console.WriteLine(reader.LocalName);
            reader.ReadStartElement();
            Console.WriteLine(reader.NodeType);

            reader.MoveToContent();
			Console.WriteLine(reader.NodeType);

			while (reader.NodeType != XmlNodeType.EndElement && reader.NodeType != 0)
			{
				if (reader.NodeType == XmlNodeType.Element)
				{
					Console.WriteLine(reader.ReadElementString());
					//if (!array[0] && (object)reader.LocalName == id3_Name && (object)reader.NamespaceURI == id2_Item)
					//{
					//	timekeeperSimpleProp.Name = reader.ReadElementString();
					//	array[0] = true;
					//}
					//else if (!array[1] && (object)reader.LocalName == id4_Age && (object)reader.NamespaceURI == id2_Item)
					//{
					//	timekeeperSimpleProp.Age = XmlConvert.ToInt32(reader.ReadElementString());
					//	array[1] = true;
					//}
					//else
					//{
					//	UnknownNode(timekeeperSimpleProp, ":Name, :Age");
					//}
				}
				else
				{
					//UnknownNode(timekeeperSimpleProp, ":Name, :Age");
				}
				reader.MoveToContent();
				//CheckReaderCount(ref whileIterations, ref readerCount);
			}


			Console.WriteLine("Done");
            Console.ReadKey();
        }
    }
}
