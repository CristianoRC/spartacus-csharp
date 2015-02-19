/*
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

namespace Spartacus.Reporting
{
    /// <summary>
    /// Alinhamento do Campo.
    /// </summary>
    public enum FieldAlignment
    {
        LEFT,
        RIGHT,
        CENTER
    }

    /// <summary>
    /// Classe Field.
    /// Representa um campo de dados do relatório.
    /// </summary>
    public class Field
    {
        /// <summary>
        /// Título do campo (aparece no cabeçalho de dados).
        /// </summary>
        public string v_title;

        /// <summary>
        /// Coluna da tabela do relatório associada ao campo.
        /// </summary>
        public string v_column;

        /// <summary>
        /// Alinhamento do campo.
        /// </summary>
        public Spartacus.Reporting.FieldAlignment v_align;

        /// <summary>
        /// Percentual indicando quanto da largura da página o campo ocupa.
        /// </summary>
        public int v_fill;

        /// <summary>
        /// Tipo de dados do campo.
        /// </summary>
        public Spartacus.Database.Type v_type;

        /// <summary>
        /// Se o campo é um valor totalizado dentro de um grupo.
        /// </summary>
        public bool v_groupedvalue;


        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="Spartacus.Reporting.Field"/>.
        /// </summary>
        public Field()
        {
            this.v_groupedvalue = false;
        }

        /// <summary>
        /// Configura o tipo de dados do campo.
        /// </summary>
        /// <param name="p_text">Texto representando o tipo de dados.</param>
        public void SetType(string p_text)
        {
            switch (p_text)
            {
                case "INTEGER":
                    this.v_type = Spartacus.Database.Type.INTEGER;
                    break;
                case "REAL":
                    this.v_type = Spartacus.Database.Type.REAL;
                    break;
                case "BOOLEAN":
                    this.v_type = Spartacus.Database.Type.BOOLEAN;
                    break;
                case "CHAR":
                    this.v_type = Spartacus.Database.Type.CHAR;
                    break;
                case "DATE":
                    this.v_type = Spartacus.Database.Type.DATE;
                    break;
                case "STRING":
                    this.v_type = Spartacus.Database.Type.STRING;
                    break;
                default:
                    this.v_type = Spartacus.Database.Type.STRING;
                    break;
            }
        }

        /// <summary>
        /// Formata o valor do campo.
        /// </summary>
        /// <param name="p_text">Texto representando o valor do campo.</param>
        public string Format(string p_text)
        {
            if (this.v_type == Spartacus.Database.Type.REAL)
                return string.Format("{0:###,###,###,###,##0.00}", double.Parse(p_text.Replace('.', ',')));
            else
                return p_text;
        }

        /// <summary>
        /// Corta o texto para caber dentro da célula.
        /// </summary>
        /// <param name="p_text">Texto.</param>
        /// <param name="p_graphics">Objeto auxiliar para renderização de texto.</param>
        /// <param name="p_font">Fonte nativa.</param>
        /// <param name="p_maxsize">Tamanho da célula.</param>
        public static string Crop(string p_text, System.Drawing.Graphics p_graphics, System.Drawing.Font p_font, double p_maxsize)
        {
            double v_maxsize;
            int v_numchars;
            double v_renderedsize;

            if (!string.IsNullOrEmpty(p_text))
            {
                v_maxsize = p_maxsize * 1.25;
                v_numchars = p_text.Length;

                v_renderedsize = p_graphics.MeasureString(p_text, p_font).Width;

                if (v_renderedsize > v_maxsize)
                    return p_text.Substring(0, (int)(((double)(v_maxsize * v_numchars)) / v_renderedsize));
                else
                    return p_text;
            }
            else
                return "";
        }
    }
}
