using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lexer
{
    public enum nodesTypes
    {
        node,
        token,
        procedure_idn,
        var_declar,
        declar_list,
        attribute,
        var_idn,
        conditional_statement,
        incomplete_conditional_statement,
        alternative_part,
        conditional_expression,
        expression,
        ///////////////////////////
        program,
        segment_list,
        segment_declar,
        block,
        declaration_list,
        statement_list,
        declaration,
        format_part,
        statement,
        fsub_statement,
        base_index_adress,
        register32,
        register16,
        fmul_statement,
        identifier,
        identifier_st,
        fcomp_statement,
        constant,
        segment_identifier,
        end_program,
        entry_point,
        entry_point_identifier,
        st_argument,
        fldz_statement
    }
    class SyntaxAnalizer
    {
        public SyntaxAnalizer(List<LexicalAnalizerOutput> lexems, List<Constant> constants, List<Identifier> identifiers, List<KeyWord> keyWords)
        {
            errors = new List<Error>();
            this.lexems = lexems;
            this.constants = constants;
            this.identifiers = identifiers;
            this.identifiersExtended = new List<IdentifierExt>();
            this.keyWords = keyWords;
            graphNodes = new List<SyntaxTree.Node>();
            links = new List<SyntaxTree.Link>();

            program = new SyntaxTree.XMLNode(nodesTypes.program);
            //graphNodes.Add(new SyntaxTree.Node(nodesTypes.program));
            positionInLexems = -1;

            
        }

        private string entry_point = "";
        private List<string> stArgs = new List<string>(new string[] { "0", "1", "2", "3", "4", "5", "6", "7" });
        private List<string> reg32 = new List<string>(new string[] { "EBX", "EAX", "ESI", "ECX", "EDX", "EBP", "ESP", "EDI", "EIP" });
        private List<string> reg16 = new List<string>(new string[] { "CS", "DS", "ES", "FS", "GS", "SS" });
        private List<Error> errors;
        private List<LexicalAnalizerOutput> lexems;
        private List<Constant> constants;
        private List<Identifier> identifiers;
        private List<KeyWord> keyWords;
        private SyntaxTree.XMLNode program;
        private int positionInLexems; // current pos in lexems
        private List<IdentifierExt> identifiersExtended;

        public delegate void WorkDoneHandler(List<Error> errors, List<IdentifierExt> identifiersExt);
        public event WorkDoneHandler WorkDone;

        private List<SyntaxTree.Node> graphNodes;
        private List<SyntaxTree.Link> links;

        private void AddGraphNode(SyntaxTree.Node g)
        {
            graphNodes.Add(g);
        }

        private void AddLink(SyntaxTree.Link l)
        {
            links.Add(l);
        }

        private void CreateGraphLabels()
        {
            foreach (var item in graphNodes)
            {
                if (item.Value != "")
                    item.Label = item.Id.ToString() + " " + item.Value;
                else
                    item.Label = item.Id.ToString();
            }
        }

        private LexicalAnalizerOutput GetNextToken()
        {
            positionInLexems++;
            if (positionInLexems < lexems.Count)
                return lexems[positionInLexems];
            else return new LexicalAnalizerOutput() { code = -1, row = -1, lexem = ""}; // end of program
        }

        private bool ParseProgram()
        {
            SyntaxTree.XMLNode currentNode = program;

            if (ParseSegmentDeclar(currentNode))
            {
                if (ParseSegmentList(currentNode))
                {
                    return true;
                }
            }
            return false;
        }

        private bool ParseSegmentDeclar(SyntaxTree.XMLNode curr)
        {
            SyntaxTree.XMLNode currentNode = curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.segment_declar });
            if (ParseSegmentIdentifier(currentNode))
            {
                LexicalAnalizerOutput currentToken = GetNextToken();
                if (currentToken.lexem == "SEGMENT")
                {
                    currentNode.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = currentToken.lexem });
                    if (ParseBlock(currentNode))
                    {
                        if (ParseSegmentIdentifier(currentNode))
                        {
                            currentToken = GetNextToken();
                            if (currentToken.lexem == "ENDS")
                            {
                                currentNode.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = currentToken.lexem });
                                return true;
                            }
                            else
                                errors.Add(new Error { message = "**Error** Expected 'ENDS' keyword", row = currentToken.row });
                        }
                    }
                }
                else
                    errors.Add(new Error { message = "**Error** Expected 'SEGMENT' keyword", row = currentToken.row });
            }
            curr.nodes.Remove(currentNode);
            return false;
        }

        private bool ParseBlock(SyntaxTree.XMLNode curr)
        {
            SyntaxTree.XMLNode currentNode = curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.block });

            if (ParseStatementList(currentNode))
            {
                return true;
            }

            return false;
        }

        private bool ParseSegmentIdentifier(SyntaxTree.XMLNode curr)
        {
            LexicalAnalizerOutput expectedIdentfier = ParseIdentifier();
            if (expectedIdentfier.lexem != "")
            {
                SyntaxTree.XMLNode currentNode = curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.segment_identifier })
                                                     .AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = expectedIdentfier.lexem });
                return true;
            }
            //else
            //    errors.Add(new Error { message = "**Error** Expected identifier", row = expectedIdentfier.row });
            return false;
        }


        private bool ParseSegmentList(SyntaxTree.XMLNode curr)
        {
            SyntaxTree.XMLNode currentNode = curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.segment_list });

            if (ParseSegmentDeclar(currentNode))
                ParseSegmentList(currentNode);

            return true;
        }


        private LexicalAnalizerOutput ParseIdentifier() // return empty string if not parsed else return value
        {
            LexicalAnalizerOutput currentToken = GetNextToken();
            if (identifiers.Find(x => x.id == currentToken.code && x.type != identifierType.system) != null)
                return currentToken;
            else
            {
                //errors.Add(new Error { message = "**Error** Expected user identifier", row = currentToken.row });
                return new LexicalAnalizerOutput() { lexem = "", row = currentToken.row };
            }
        }

        

        private bool ParseStatementList(SyntaxTree.XMLNode curr)
        {
            SyntaxTree.XMLNode currentNode = curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.statement_list });

            if (ParseStatement(currentNode))
                ParseStatementList(currentNode);
            else
            {
                //positionInLexems--;
                //curr.nodes.Remove(currentNode);
            }
                

            //positionInLexems--;  
            return true;
        }

        private bool ParseStatement(SyntaxTree.XMLNode curr)
        {
            SyntaxTree.XMLNode currentNode = curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.statement });

            int currPosInLexems = positionInLexems;
            if (ParseEntryPoint(currentNode))
            {
                return true;
            }
            positionInLexems = currPosInLexems;
            if (ParseDeclaration(currentNode))
            {
                return true;
            }
            positionInLexems = currPosInLexems; // restore position in lexems
            if (ParseFLDZStatement(currentNode))
            {
                return true;
            }
            positionInLexems = currPosInLexems; // restore position in lexems
            if (ParseFSUBStatement(currentNode))
            {
                return true;
            }
            positionInLexems = currPosInLexems; // restore position in lexems
            if (ParseFCOMPStatement(currentNode))
            {
                return true;
            }
            positionInLexems = currPosInLexems; // restore position in lexems
            if (ParseENDStatement(currentNode))
            {
                return true;
            }
            positionInLexems = currPosInLexems; // restore position in lexems
            return false;
        }

        private bool ParseFCOMPStatement(SyntaxTree.XMLNode curr)
        {
            LexicalAnalizerOutput currentToken = GetNextToken();
            if (currentToken.lexem == "FCOMP")
            {
                SyntaxTree.XMLNode fcomp = curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.fcomp_statement });
                fcomp.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = currentToken.lexem });
                if (ParseIdentifierSt(fcomp))
                {
                    return true;
                }
                else
                    curr.nodes.Remove(fcomp);
            }
            return false;
        }

        private bool ParseIdentifierSt(SyntaxTree.XMLNode curr)
        {
            LexicalAnalizerOutput currentToken = GetNextToken();
            if (currentToken.lexem == "ST")
            {
                SyntaxTree.XMLNode idn_st = curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.identifier_st, value = currentToken.lexem });
                idn_st.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = currentToken.lexem });
                currentToken = GetNextToken();
                if (currentToken.lexem == "(")
                {
                    idn_st.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = currentToken.lexem });
                    currentToken = GetNextToken();
                    if (stArgs.Any(currentToken.lexem.Contains))
                    {
                        idn_st.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.st_argument, value = currentToken.lexem });
                        currentToken = GetNextToken();
                        if (currentToken.lexem == ")")
                        {
                            idn_st.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = currentToken.lexem });
                            return true;
                        }
                        else
                            errors.Add(new Error { message = "**Error** Expected ')'", row = currentToken.row });
                    }
                    else
                        errors.Add(new Error { message = "**Error** Expected right argument", row = currentToken.row });
                }
                else
                    errors.Add(new Error { message = "**Error** Expected '('", row = currentToken.row });
            }
            else
                errors.Add(new Error { message = "**Error** Expected 'ST'", row = currentToken.row });
            return false;
        }

        private bool ParseENDStatement(SyntaxTree.XMLNode curr)
        {
            LexicalAnalizerOutput currentToken = GetNextToken();
            if (currentToken.lexem == "END")
            {
                currentToken = GetNextToken();
                if (currentToken.lexem == entry_point)
                {
                    curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.end_program });
                    return true;
                }
                else
                    errors.Add(new Error { message = "**Error** Expected entry point identifier", row = currentToken.row });
            }
            return false;
        }

        private bool ParseFSUBStatement(SyntaxTree.XMLNode curr)
        {
            LexicalAnalizerOutput currentToken = GetNextToken();
            if (currentToken.lexem == "FSUB")
            {
                SyntaxTree.XMLNode fsub = curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.fsub_statement });
                LexicalAnalizerOutput expectedIdentifier = ParseIdentifier();
                if (expectedIdentifier.lexem != "")
                {
                    if (identifiersExtended.Exists(x => x.name == expectedIdentifier.lexem))
                    {
                        fsub.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.identifier })
                            .AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = expectedIdentifier.lexem });
                        currentToken = GetNextToken();
                        if (currentToken.lexem == "[")
                        {
                            fsub.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = currentToken.lexem });
                            int currPosInLexems = positionInLexems;
                            if (!ParseReg16(fsub))
                            {
                                positionInLexems = currPosInLexems; // restore position in lexems
                                if (!ParseReg32(fsub))
                                {
                                    positionInLexems = currPosInLexems;
                                    currentToken = GetNextToken();
                                    errors.Add(new Error { message = "**Error** Expected register", row = currentToken.row });
                                    return false;
                                }
                            }
                            // if registers successfully parsed
                            
                            currentToken = GetNextToken();
                            if (currentToken.lexem == "+")
                            {
                                fsub.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = currentToken.lexem });
                                currPosInLexems = positionInLexems;
                                if (!ParseReg16(fsub))
                                {
                                    positionInLexems = currPosInLexems; // restore position in lexems
                                    if (!ParseReg32(fsub))
                                    {
                                        positionInLexems = currPosInLexems;
                                        currentToken = GetNextToken();
                                        errors.Add(new Error { message = "**Error** Expected register", row = currentToken.row });
                                        return false;
                                    }
                                }
                                // if registers successfully parsed
                                
                                currentToken = GetNextToken();
                                if (currentToken.lexem == "]")
                                {
                                    fsub.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = currentToken.lexem });
                                    return true;
                                }
                                else
                                    errors.Add(new Error { message = "**Error** Expected ']'", row = currentToken.row });
                                
                            }
                            else
                                errors.Add(new Error { message = "**Error** Expected '+'", row = currentToken.row }); 
                            
                        }
                        else
                            errors.Add(new Error { message = "**Error** Expected '['", row = currentToken.row });
                    }
                    else
                        errors.Add(new Error { message = "**Error** Identifier is not declared", row = expectedIdentifier.row });
                }
                else
                    errors.Add(new Error { message = "**Error** Expected identifier", row = expectedIdentifier.row });
                curr.nodes.Remove(fsub); 
            }
            return false;
        }

        private bool ParseFLDZStatement(SyntaxTree.XMLNode curr)
        {
            LexicalAnalizerOutput currentToken = GetNextToken();
            if (currentToken.lexem == "FLDZ")
            {
                curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.fldz_statement });
                return true;
            }
            return false;
        }

        private bool ParseReg16(SyntaxTree.XMLNode curr)
        {
            LexicalAnalizerOutput currentToken = GetNextToken();
            if (reg16.Any(currentToken.lexem.Contains))
            {
                curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.register16, value = currentToken.lexem });
                return true;
            }
            return false;
        }

        private bool ParseReg32(SyntaxTree.XMLNode curr)
        {
            LexicalAnalizerOutput currentToken = GetNextToken();
            if (reg32.Any(currentToken.lexem.Contains))
            {
                curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.register32, value = currentToken.lexem });
                return true;
            }
            return false;
        }

        private bool ParseDeclaration(SyntaxTree.XMLNode curr)
        {
            LexicalAnalizerOutput expectedIdentifier = ParseIdentifier();

            if (expectedIdentifier.lexem != "")
            {
                LexicalAnalizerOutput currentToken = GetNextToken();
                if (currentToken.lexem == "DD" || currentToken.lexem == "DQ")
                {
                    SyntaxTree.XMLNode declaration = curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.declaration });
                    declaration.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.identifier })
                               .AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = expectedIdentifier.lexem });
                    SyntaxTree.XMLNode format_part = declaration.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.format_part });
                    format_part.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = currentToken.lexem });

                    if (ParseConstant(format_part))
                    {
                        identifiersExtended.Add(new IdentifierExt() { name = expectedIdentifier.lexem, typeAttribute = currentToken.lexem }); // store declared idn 
                        return true;
                    }    
                    else
                    {
                        curr.nodes.Remove(declaration);
                    }
                }
            }
            return false;
        }

        private bool ParseConstant(SyntaxTree.XMLNode curr)
        {
            LexicalAnalizerOutput currentToken = GetNextToken();
            if (constants.Exists(x => x.id == currentToken.code))
            {
                curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.constant });
                return true;
            }
            errors.Add(new Error { message = "**Error** Expected constant", row = currentToken.row });
            return false;
        }

        private bool ParseEntryPoint(SyntaxTree.XMLNode curr)
        {
            LexicalAnalizerOutput expectedIdentifier = ParseIdentifier();

            if (expectedIdentifier.lexem != "")
            {
                LexicalAnalizerOutput currentToken = GetNextToken();
                if (currentToken.lexem == ":")
                {
                    SyntaxTree.XMLNode currentNode = curr.AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.entry_point })
                                                         .AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.entry_point_identifier })
                                                         .AddNode(new SyntaxTree.XMLNode() { name = nodesTypes.token, value = expectedIdentifier.lexem });
                    entry_point = expectedIdentifier.lexem;
                    return true;
                }
            }
            return false;
        }




        public void Analize()
        {
            ParseProgram();
            SerializeTables.SeriaizeNode(program);

            SyntaxTree.XMLNodeToDGMLParser parser = new SyntaxTree.XMLNodeToDGMLParser();
            SyntaxTree.Graph graph = parser.GetGraph();
            
            SerializeTables.SeriaizeNodeGraph(graph);
            if (WorkDone != null) WorkDone(errors, identifiersExtended);
        }
    }
}
