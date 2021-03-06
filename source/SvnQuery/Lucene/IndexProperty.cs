﻿#region Apache License 2.0

// Copyright 2008-2009 Christian Rodemeyer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using SvnQuery.Svn;

namespace SvnQuery.Lucene
{
    public static class IndexProperty
    {
        const string RevisionProperty = "Revision";
        const string RepositoryNameProperty = "RepositoryName";
        const string RepositoryLocalUriProperty = "RepositoryLocalUri";
        const string RepositoryExternalUriProperty = "RepositoryExternalUri";
        const string RepositoryCredentialsProperty = "RepositoryCredentials";
        const string SingleRevisionProperty = "SingleRevision";

        // Lucene fields for property storage
        const string IdField = "$Property";
        const string ValueField = "$Value";

        /// <summary>
        /// returns a term that uniquely identifies a document containing an index property
        /// </summary>        
        static Term GetPropertyId(string property)
        {
            return new Term(IdField, property);
        }

        static string GetProperty(IndexReader reader, string property)
        {
            TermDocs td = reader.TermDocs(GetPropertyId(property));
            if (!td.Next()) return null;
            return reader.Document(td.Doc()).Get(ValueField);
        }

        static void UpdateProperty(IndexWriter writer, string property, string value)
        {
            var doc = new Document();
            doc.Add(new Field(IdField, property, Field.Store.NO, Field.Index.UN_TOKENIZED));
            doc.Add(new Field(ValueField, value, Field.Store.YES, Field.Index.NO));
            writer.UpdateDocument(GetPropertyId(property), doc);
        }

        public static int GetRevision(IndexReader reader)
        {
            return int.Parse(GetProperty(reader, RevisionProperty));
        }

        public static void SetRevision(IndexWriter writer, int revision)
        {
            UpdateProperty(writer, RevisionProperty, revision.ToString());
        }

        public static string GetRepositoryName(IndexReader reader)
        {
            return GetProperty(reader, RepositoryNameProperty);
        }

        public static void SetRepositoryName(IndexWriter writer, string name)
        {
            UpdateProperty(writer, RepositoryNameProperty, name);
        }

        public static string GetRepositoryLocalUri(IndexReader reader)
        {
            return GetProperty(reader, RepositoryLocalUriProperty);
        }

        public static void SetRepositoryLocalUri(IndexWriter writer, string uri)
        {
            UpdateProperty(writer, RepositoryLocalUriProperty, uri);
        }

        public static Credentials GetRepositoryCredentials(IndexReader reader)
        {
            return new Credentials(GetProperty(reader, RepositoryCredentialsProperty));
        }

        public static void SetRepositoryCredentials(IndexWriter writer, Credentials credentials)
        {
            UpdateProperty(writer, RepositoryCredentialsProperty, credentials.ToString());
        }

        public static string GetRepositoryExternalUri(IndexReader reader)
        {
            return GetProperty(reader, RepositoryExternalUriProperty);
        }

        public static void SetRepositoryExternalUri(IndexWriter writer, string uri)
        {
            UpdateProperty(writer, RepositoryExternalUriProperty, uri);  
        }

        public static bool GetSingleRevision(IndexReader reader)
        {            
            return bool.Parse(GetProperty(reader, SingleRevisionProperty) ?? "False");
        }

        public static void SetSingleRevision(IndexWriter writer, bool isSingleRevision)
        {
            UpdateProperty(writer, SingleRevisionProperty, isSingleRevision.ToString());
        }

    }
}