﻿/*
The MIT License (MIT)

Copyright (c) 2014,2015 William Ivanski

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Data;
using Mono.Data.Sqlite;

namespace Spartacus.Database
{
    /// <summary>
    /// Classe Spartacus.Database.XbaseReadOnly.
    /// Herda da classe <see cref="Spartacus.Database.Generic"/>.
    /// Converte um ou mais arquivos DBF para um banco de dados SQLite.
    /// Depois utiliza o Mono.Data.Sqlite para acessar esse banco de dados.
    /// </summary>
    public class XbaseReadOnly : Spartacus.Database.Generic
    {
        /// <summary>
        /// String de conexão para acessar o banco.
        /// </summary>
        private string v_connectionstring;

        /// <summary>
        /// Conexão com o banco de dados.
        /// </summary>
        private Mono.Data.Sqlite.SqliteConnection v_con;

        /// <summary>
        /// Comando para conexão com o banco de dados.
        /// </summary>
        private Mono.Data.Sqlite.SqliteCommand v_cmd;

        /// <summary>
        /// Leitor de dados do banco de dados.
        /// </summary>
        private Mono.Data.Sqlite.SqliteDataReader v_reader;

        /// <summary>
        /// Linha atual da QueryBlock.
        /// </summary>
        private uint v_currentrow;

        /// <summary>
        /// Lista de arquivos DBF de origem a serem convertidos para SQLite
        /// </summary>
        private System.Collections.ArrayList v_sourcefiles;


        /// <summary>
        /// Inicializa uma nova instancia da classe <see cref="Spartacus.Database.XbaseReadOnly"/>.
        /// </summary>
        /// <param name='p_filelist'>
        /// Lista de arquivos DBF de origem a serem convertidos para SQLite
        /// </param>
        public XbaseReadOnly(string p_filelist)
            : base(p_filelist)
        {
            this.v_sourcefiles = new System.Collections.ArrayList();

            foreach (string v_file in p_filelist.Split(','))
                this.v_sourcefiles.Add(v_file);

            this.v_con = null;
            this.v_cmd = null;
            this.v_reader = null;
        }

        /// <summary>
        /// Cria um banco de dados SQLite contendo todos os arquivos DBF, um em cada tabela.
        /// </summary>
        /// <param name="p_name">Nome do arquivo de banco de dados a ser criado.</param>
        public override void CreateDatabase(string p_name)
        {
            System.Diagnostics.Process v_process;
            System.Diagnostics.ProcessStartInfo v_startinfo;
            Spartacus.Utils.File v_file;
            Spartacus.Utils.Excel v_excel;
            string v_tablename, v_sql;

            try
            {
                // criando banco de dados de destino
                Mono.Data.Sqlite.SqliteConnection.CreateFile(p_name);
                this.v_service = p_name;
                this.v_connectionstring = "Data Source=" + p_name + ";Version=3;Synchronous=Full;Journal Mode=Off;";

                // criando processo de conversão de DBF para CSV
                v_process = new System.Diagnostics.Process();
                v_startinfo = new System.Diagnostics.ProcessStartInfo();
                v_startinfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                if (System.Environment.OSVersion.Platform == System.PlatformID.Unix)
                    v_startinfo.FileName = "dbf";
                else
                    v_startinfo.FileName = "dbf.exe";

                v_excel = new Spartacus.Utils.Excel();
                this.Open();

                for (int k = 0; k < this.v_sourcefiles.Count; k++)
                {
                    v_file = new Spartacus.Utils.File(1, 1, Spartacus.Utils.FileType.FILE, (string) this.v_sourcefiles[k]);
                    v_tablename = v_file.GetBaseNameNoExt().ToLower();
                    v_startinfo.Arguments = " --separator \";\" --csv " + v_tablename + ".csv " + v_file.v_name;
                    v_process.StartInfo = v_startinfo;
                    v_process.Start();
                    v_process.WaitForExit();

                    v_excel.Clear();
                    v_excel.Import(v_tablename + ".csv", ';', '"', true, System.Text.Encoding.UTF8);
                    (new System.IO.FileInfo(v_tablename + ".csv")).Delete();

                    if (v_excel.v_set.Tables[0] != null && v_excel.v_set.Tables[0].Columns.Count > 0)
                    {
                        // arrumando nomes de colunas
                        for (int j = 0; j < v_excel.v_set.Tables[0].Columns.Count; j++)
                            v_excel.v_set.Tables[0].Columns[j].ColumnName = v_excel.v_set.Tables[0].Columns[j].ColumnName.ToLower().Split(',')[0];

                        // criando tabela
                        v_sql = "create table " + v_tablename + "(" + v_excel.v_set.Tables[0].Columns[0].ColumnName.ToLower() + " text";
                        for (int j = 1; j < v_excel.v_set.Tables[0].Columns.Count; j++)
                            v_sql += ", " + v_excel.v_set.Tables[0].Columns[j].ColumnName.ToLower() + " text";
                        v_sql += ");";
                        this.Execute(v_sql);

                        // inserindo dados
                        foreach (System.Data.DataRow v_row in v_excel.v_set.Tables[0].Rows)
                        {
                            v_sql = "insert into " + v_tablename + " values ('" + Spartacus.Database.Command.RemoveUnwantedChars(v_row[0].ToString());
                            for (int j = 1; j < v_excel.v_set.Tables[0].Columns.Count; j++)
                                v_sql += "', '" + Spartacus.Database.Command.RemoveUnwantedChars(v_row[j].ToString());
                            v_sql += "');";
                            this.Execute(v_sql);
                        }
                    }
                }

                this.Close();
            }
            catch (Spartacus.Database.Exception e)
            {
                throw e;
            }
            catch (Mono.Data.Sqlite.SqliteException e)
            {
                throw new Spartacus.Database.Exception(e);
            }
            catch (System.Exception e)
            {
                throw new Spartacus.Database.Exception(e);
            }
        }

        /// <summary>
        /// Cria um banco de dados.
        /// </summary>
        public override void CreateDatabase()
        {
        }

        /// <summary>
        /// Abre a conexão com o banco de dados.
        /// </summary>
        public override void Open()
        {
            try
            {
                this.v_con = new Mono.Data.Sqlite.SqliteConnection(this.v_connectionstring);
                this.v_con.Open();
                this.v_cmd = new Mono.Data.Sqlite.SqliteCommand();
                this.v_cmd.Connection = this.v_con;
            }
            catch (Mono.Data.Sqlite.SqliteException e)
            {
                throw new Spartacus.Database.Exception(e);
            }
        }

        /// <summary>
        /// Realiza uma consulta no banco de dados, armazenando os dados de retorno em um <see cref="System.Data.DataTable"/>.
        /// </summary>
        /// <param name='p_sql'>
        /// Código SQL a ser consultado no banco de dados.
        /// </param>
        /// <param name='p_tablename'>
        /// Nome virtual da tabela onde deve ser armazenado o resultado, para fins de cache.
        /// </param>
        /// <returns>Retorna uma <see cref="System.Data.DataTable"/> com os dados de retorno da consulta.</returns>
        public override System.Data.DataTable Query(string p_sql, string p_tablename)
        {
            System.Data.DataTable v_table = null;
            System.Data.DataRow v_row;

            if (this.v_con == null)
            {
                try
                {
                    this.v_con = new Mono.Data.Sqlite.SqliteConnection(this.v_connectionstring);
                    this.v_con.Open();
                    this.v_cmd = new Mono.Data.Sqlite.SqliteCommand(p_sql, this.v_con);
                    this.v_reader = this.v_cmd.ExecuteReader();

                    v_table = new System.Data.DataTable(p_tablename);
                    for (int i = 0; i < v_reader.FieldCount; i++)
                        v_table.Columns.Add(this.FixColumnName(this.v_reader.GetName(i)), typeof(string));

                    while (this.v_reader.Read())
                    {
                        v_row = v_table.NewRow();
                        for (int i = 0; i < this.v_reader.FieldCount; i++)
                            v_row[i] = this.v_reader[i].ToString();
                        v_table.Rows.Add(v_row);
                    }

                    return v_table;
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
                finally
                {
                    if (this.v_reader != null)
                    {
                        this.v_reader.Close();
                        this.v_reader = null;
                    }
                    if (this.v_cmd != null)
                    {
                        this.v_cmd.Dispose();
                        this.v_cmd = null;
                    }
                    if (this.v_con != null)
                    {
                        this.v_con.Close();
                        this.v_con = null;
                    }
                }
            }
            else
            {
                try
                {
                    this.v_cmd.CommandText = p_sql;
                    this.v_reader = this.v_cmd.ExecuteReader();

                    v_table = new System.Data.DataTable(p_tablename);
                    for (int i = 0; i < v_reader.FieldCount; i++)
                        v_table.Columns.Add(this.FixColumnName(this.v_reader.GetName(i)), typeof(string));

                    while (this.v_reader.Read())
                    {
                        v_row = v_table.NewRow();
                        for (int i = 0; i < this.v_reader.FieldCount; i++)
                            v_row[i] = this.v_reader[i].ToString();
                        v_table.Rows.Add(v_row);
                    }

                    return v_table;
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
                finally
                {
                    if (this.v_reader != null)
                    {
                        this.v_reader.Close();
                        this.v_reader = null;
                    }
                }
            }
        }

        /// <summary>
        /// Realiza uma consulta no banco de dados, armazenando os dados de retorno em um <see creg="System.Data.DataTable"/>.
        /// Utiliza um DataReader para buscar em blocos. Conexão com o banco precisa estar aberta.
        /// </summary>
        /// <param name='p_sql'>
        /// Código SQL a ser consultado no banco de dados.
        /// </param>
        /// <param name='p_tablename'>
        /// Nome virtual da tabela onde deve ser armazenado o resultado, para fins de cache.
        /// </param>
        /// <param name='p_startrow'>
        /// Número da linha inicial.
        /// </param>
        /// <param name='p_endrow'>
        /// Número da linha final.
        /// </param>
        /// <param name='p_hasmoredata'>
        /// Indica se ainda há mais dados a serem lidos.
        /// </param>
        public override System.Data.DataTable Query(string p_sql, string p_tablename, uint p_startrow, uint p_endrow, out bool p_hasmoredata)
        {
            System.Data.DataTable v_table = null;
            System.Data.DataRow v_row;

            try
            {
                if (this.v_reader == null)
                {
                    this.v_cmd.CommandText = p_sql;
                    this.v_reader = this.v_cmd.ExecuteReader();
                    this.v_currentrow = 0;
                }

                v_table = new System.Data.DataTable(p_tablename);
                for (int i = 0; i < v_reader.FieldCount; i++)
                    v_table.Columns.Add(this.FixColumnName(this.v_reader.GetName(i)), typeof(string));

                while (this.v_reader.Read())
                {
                    if (this.v_currentrow >= p_startrow && this.v_currentrow <= p_endrow)
                    {
                        v_row = v_table.NewRow();
                        for (int i = 0; i < this.v_reader.FieldCount; i++)
                            v_row[i] = this.v_reader[i].ToString();
                        v_table.Rows.Add(v_row);
                    }

                    if (this.v_currentrow > p_endrow)
                        break;

                    this.v_currentrow++;
                }

                if (this.v_currentrow > p_endrow)
                {
                    this.v_reader.Close();
                    this.v_reader = null;
                    p_hasmoredata = false;
                }
                else
                    p_hasmoredata = true;

                return v_table;
            }
            catch (Mono.Data.Sqlite.SqliteException e)
            {
                throw new Spartacus.Database.Exception(e);
            }
        }

        /// <summary>
        /// Executa um código SQL no banco de dados.
        /// </summary>
        /// <param name='p_sql'>
        /// Código SQL a ser executado no banco de dados.
        /// </param>
        public override void Execute(string p_sql)
        {
            if (this.v_con == null)
            {
                try
                {
                    this.v_con = new Mono.Data.Sqlite.SqliteConnection(this.v_connectionstring);
                    this.v_con.Open();
                    this.v_cmd = new Mono.Data.Sqlite.SqliteCommand(Spartacus.Database.Command.RemoveUnwantedCharsExecute(p_sql), this.v_con);
                    this.v_cmd.ExecuteNonQuery();
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
                finally
                {
                    if (this.v_cmd != null)
                    {
                        this.v_cmd.Dispose();
                        this.v_cmd = null;
                    }
                    if (this.v_con != null)
                    {
                        this.v_con.Close();
                        this.v_con = null;
                    }
                }
            }
            else
            {
                try
                {
                    this.v_cmd.CommandText = Spartacus.Database.Command.RemoveUnwantedCharsExecute(p_sql);
                    this.v_cmd.ExecuteNonQuery();
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
            }
        }

        /// <summary>
        /// Realiza uma consulta no banco de dados, armazenando um único dado de retorno em uma string.
        /// </summary>
        /// <returns>
        /// string com o dado de retorno.
        /// </returns>
        /// <param name='p_sql'>
        /// Código SQL a ser consultado no banco de dados.
        /// </param>
        public override string ExecuteScalar(string p_sql)
        {
            if (this.v_con == null)
            {
                try
                {
                    this.v_con = new Mono.Data.Sqlite.SqliteConnection(this.v_connectionstring);
                    this.v_con.Open();
                    this.v_cmd = new Mono.Data.Sqlite.SqliteCommand(Spartacus.Database.Command.RemoveUnwantedCharsExecute(p_sql), this.v_con);
                    return (string) this.v_cmd.ExecuteScalar();
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
                finally
                {
                    if (this.v_cmd != null)
                    {
                        this.v_cmd.Dispose();
                        this.v_cmd = null;
                    }
                    if (this.v_con != null)
                    {
                        this.v_con.Close();
                        this.v_con = null;
                    }
                }
            }
            else
            {
                try
                {
                    this.v_cmd.CommandText = Spartacus.Database.Command.RemoveUnwantedCharsExecute(p_sql);
                    return (string) this.v_cmd.ExecuteScalar();
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
            }
        }

        /// <summary>
        /// Fecha a conexão com o banco de dados.
        /// </summary>
        public override void Close()
        {
            if (this.v_cmd != null)
            {
                this.v_cmd.Dispose();
                this.v_cmd = null;
            }
            if (this.v_con != null)
            {
                this.v_con.Close();
                this.v_con = null;
            }
        }

        /// <summary>
        /// Deleta um banco de dados.
        /// </summary>
        /// <param name="p_name">Nome do banco de dados a ser deletado.</param>
        public override void DropDatabase(string p_name)
        {
        }

        /// <summary>
        /// Deleta o banco de dados conectado atualmente.
        /// </summary>
        public override void DropDatabase()
        {
        }

        /// <summary>
        /// Transfere dados do banco de dados atual para um banco de dados de destino.
        /// Conexão com o banco de destino precisa estar aberta.
        /// </summary>
        /// <returns>Número de linhas transferidas.</returns>
        /// <param name="p_query">Consulta SQL para buscar os dados no banco atual.</param>
        /// <param name="p_insert">Comando de inserção para inserir cada linha no banco de destino.</param>
        /// <param name="p_destdatabase">Conexão com o banco de destino.</param>
        public override int Transfer(string p_query, string p_insert, Spartacus.Database.Generic p_destdatabase)
        {
            int v_transfered = 0;
            string v_insert;

            if (this.v_con == null)
            {
                try
                {
                    this.v_con = new Mono.Data.Sqlite.SqliteConnection(this.v_connectionstring);
                    this.v_con.Open();
                    this.v_cmd = new Mono.Data.Sqlite.SqliteCommand(p_query, this.v_con);
                    this.v_reader = this.v_cmd.ExecuteReader();

                    while (v_reader.Read())
                    {
                        v_insert = p_insert;
                        for (int i = 0; i < v_reader.FieldCount; i++)
                            v_insert = v_insert.Replace("#" + this.FixColumnName(v_reader.GetName(i)).ToLower() + "#", v_reader[i].ToString());

                        p_destdatabase.Execute(v_insert);
                        v_transfered++;
                    }

                    return v_transfered;
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
                finally
                {
                    if (this.v_reader != null)
                    {
                        this.v_reader.Close();
                        this.v_reader = null;
                    }
                    if (this.v_cmd != null)
                    {
                        this.v_cmd.Dispose();
                        this.v_cmd = null;
                    }
                    if (this.v_con != null)
                    {
                        this.v_con.Close();
                        this.v_con = null;
                    }
                }
            }
            else
            {
                try
                {
                    this.v_cmd.CommandText = p_query;
                    this.v_reader = this.v_cmd.ExecuteReader();

                    while (v_reader.Read())
                    {
                        v_insert = p_insert;
                        for (int i = 0; i < v_reader.FieldCount; i++)
                            v_insert = v_insert.Replace("#" + this.FixColumnName(v_reader.GetName(i)).ToLower() + "#", v_reader[i].ToString());

                        p_destdatabase.Execute(v_insert);
                        v_transfered++;
                    }

                    return v_transfered;
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
                finally
                {
                    if (this.v_reader != null)
                    {
                        this.v_reader.Close();
                        this.v_reader = null;
                    }
                }
            }
        }

        /// <summary>
        /// Transfere dados do banco de dados atual para um banco de dados de destino.
        /// Conexão com o banco de destino precisa estar aberta.
        /// </summary>
        /// <returns>Número de linhas transferidas.</returns>
        /// <param name="p_query">Consulta SQL para buscar os dados no banco atual.</param>
        /// <param name="p_insert">Comando de inserção para inserir cada linha no banco de destino.</param>
        /// <param name="p_destdatabase">Conexão com o banco de destino.</param>
        public override int Transfer(string p_query, Spartacus.Database.Command p_insert, Spartacus.Database.Generic p_destdatabase)
        {
            int v_transfered = 0;

            if (this.v_con == null)
            {
                try
                {
                    this.v_con = new Mono.Data.Sqlite.SqliteConnection(this.v_connectionstring);
                    this.v_con.Open();
                    this.v_cmd = new Mono.Data.Sqlite.SqliteCommand(p_query, this.v_con);
                    this.v_reader = this.v_cmd.ExecuteReader();

                    while (v_reader.Read())
                    {
                        for (int i = 0; i < v_reader.FieldCount; i++)
                            p_insert.SetValue(this.FixColumnName(v_reader.GetName(i)).ToLower(), v_reader[i].ToString());

                        p_destdatabase.Execute(p_insert.GetUpdatedText());
                        v_transfered++;
                    }

                    return v_transfered;
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
                finally
                {
                    if (this.v_reader != null)
                    {
                        this.v_reader.Close();
                        this.v_reader = null;
                    }
                    if (this.v_cmd != null)
                    {
                        this.v_cmd.Dispose();
                        this.v_cmd = null;
                    }
                    if (this.v_con != null)
                    {
                        this.v_con.Close();
                        this.v_con = null;
                    }
                }
            }
            else
            {
                try
                {
                    this.v_cmd.CommandText = p_query;
                    this.v_reader = this.v_cmd.ExecuteReader();

                    while (v_reader.Read())
                    {
                        for (int i = 0; i < v_reader.FieldCount; i++)
                            p_insert.SetValue(this.FixColumnName(v_reader.GetName(i)).ToLower(), v_reader[i].ToString());

                        p_destdatabase.Execute(p_insert.GetUpdatedText());
                        v_transfered++;
                    }

                    return v_transfered;
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
                finally
                {
                    if (this.v_reader != null)
                    {
                        this.v_reader.Close();
                        this.v_reader = null;
                    }
                }
            }
        }

        /// <summary>
        /// Transfere dados do banco de dados atual para um banco de dados de destino.
        /// Conexão com o banco de destino precisa estar aberta.
        /// Não pára a execução se der um problema num comando de inserção específico.
        /// </summary>
        /// <returns>Número de linhas transferidas.</returns>
        /// <param name="p_query">Consulta SQL para buscar os dados no banco atual.</param>
        /// <param name="p_insert">Comando de inserção para inserir cada linha no banco de destino.</param>
        /// <param name="p_destdatabase">Conexão com o banco de destino.</param>
        /// <param name="p_log">Log de inserção.</param>
        public override int Transfer(string p_query, string p_insert, Spartacus.Database.Generic p_destdatabase, out string p_log)
        {
            int v_transfered = 0;
            string v_insert;

            p_log = "";

            if (this.v_con == null)
            {
                try
                {
                    this.v_con = new Mono.Data.Sqlite.SqliteConnection(this.v_connectionstring);
                    this.v_con.Open();
                    this.v_cmd = new Mono.Data.Sqlite.SqliteCommand(p_query, this.v_con);
                    this.v_reader = this.v_cmd.ExecuteReader();

                    while (v_reader.Read())
                    {
                        v_insert = p_insert;
                        for (int i = 0; i < v_reader.FieldCount; i++)
                            v_insert = v_insert.Replace("#" + this.FixColumnName(v_reader.GetName(i)).ToLower() + "#", v_reader[i].ToString());

                        try
                        {
                            p_destdatabase.Execute(v_insert);
                            v_transfered++;
                        }
                        catch (Spartacus.Database.Exception e)
                        {
                            p_log += v_insert + "\n" + e.v_message + "\n";
                        }
                    }

                    return v_transfered;
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
                finally
                {
                    if (this.v_reader != null)
                    {
                        this.v_reader.Close();
                        this.v_reader = null;
                    }
                    if (this.v_cmd != null)
                    {
                        this.v_cmd.Dispose();
                        this.v_cmd = null;
                    }
                    if (this.v_con != null)
                    {
                        this.v_con.Close();
                        this.v_con = null;
                    }
                }
            }
            else
            {
                try
                {
                    this.v_cmd.CommandText = p_query;
                    this.v_reader = this.v_cmd.ExecuteReader();

                    while (v_reader.Read())
                    {
                        v_insert = p_insert;
                        for (int i = 0; i < v_reader.FieldCount; i++)
                            v_insert = v_insert.Replace("#" + this.FixColumnName(v_reader.GetName(i)).ToLower() + "#", v_reader[i].ToString());

                        try
                        {
                            p_destdatabase.Execute(v_insert);
                            v_transfered++;
                        }
                        catch (Spartacus.Database.Exception e)
                        {
                            p_log += v_insert + "\n" + e.v_message + "\n";
                        }
                    }

                    return v_transfered;
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
                finally
                {
                    if (this.v_reader != null)
                    {
                        this.v_reader.Close();
                        this.v_reader = null;
                    }
                }
            }
        }

        /// <summary>
        /// Transfere dados do banco de dados atual para um banco de dados de destino.
        /// Conexão com o banco de destino precisa estar aberta.
        /// Não pára a execução se der um problema num comando de inserção específico.
        /// </summary>
        /// <returns>Número de linhas transferidas.</returns>
        /// <param name="p_query">Consulta SQL para buscar os dados no banco atual.</param>
        /// <param name="p_insert">Comando de inserção para inserir cada linha no banco de destino.</param>
        /// <param name="p_destdatabase">Conexão com o banco de destino.</param>
        /// <param name="p_log">Log de inserção.</param>
        public override int Transfer(string p_query, Spartacus.Database.Command p_insert, Spartacus.Database.Generic p_destdatabase, out string p_log)
        {
            int v_transfered = 0;
            string v_insert;

            p_log = "";

            if (this.v_con == null)
            {
                try
                {
                    this.v_con = new Mono.Data.Sqlite.SqliteConnection(this.v_connectionstring);
                    this.v_con.Open();
                    this.v_cmd = new Mono.Data.Sqlite.SqliteCommand(p_query, this.v_con);
                    this.v_reader = this.v_cmd.ExecuteReader();

                    while (v_reader.Read())
                    {
                        for (int i = 0; i < v_reader.FieldCount; i++)
                            p_insert.SetValue(this.FixColumnName(v_reader.GetName(i)).ToLower(), v_reader[i].ToString());

                        v_insert = p_insert.GetUpdatedText();
                        try
                        {
                            p_destdatabase.Execute(v_insert);
                            v_transfered++;
                        }
                        catch (Spartacus.Database.Exception e)
                        {
                            p_log += v_insert + "\n" + e.v_message + "\n";
                        }
                    }

                    return v_transfered;
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
                finally
                {
                    if (this.v_reader != null)
                    {
                        this.v_reader.Close();
                        this.v_reader = null;
                    }
                    if (this.v_cmd != null)
                    {
                        this.v_cmd.Dispose();
                        this.v_cmd = null;
                    }
                    if (this.v_con != null)
                    {
                        this.v_con.Close();
                        this.v_con = null;
                    }
                }
            }
            else
            {
                try
                {
                    this.v_cmd.CommandText = p_query;
                    this.v_reader = this.v_cmd.ExecuteReader();

                    while (v_reader.Read())
                    {
                        for (int i = 0; i < v_reader.FieldCount; i++)
                            p_insert.SetValue(this.FixColumnName(v_reader.GetName(i)).ToLower(), v_reader[i].ToString());

                        v_insert = p_insert.GetUpdatedText();
                        try
                        {
                            p_destdatabase.Execute(v_insert);
                            v_transfered++;
                        }
                        catch (Spartacus.Database.Exception e)
                        {
                            p_log += v_insert + "\n" + e.v_message + "\n";
                        }
                    }

                    return v_transfered;
                }
                catch (Mono.Data.Sqlite.SqliteException e)
                {
                    throw new Spartacus.Database.Exception(e);
                }
                finally
                {
                    if (this.v_reader != null)
                    {
                        this.v_reader.Close();
                        this.v_reader = null;
                    }
                }
            }
        }
    }
}