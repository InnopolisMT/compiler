using System.Text;
using System.Collections.Generic;
using System.Text.Json;
using Compiler.Lexer;
using Compiler.Parser;
using Compiler.AST;

namespace Compiler.TreeView
{
    public class TreeVisualizer
    {
        public string GenerateTreeHtml(ProgramNode program, string title = "AST Visualization")
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"    <title>{title}</title>");
            sb.AppendLine("    <script src=\"https://d3js.org/d3.v7.min.js\"></script>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
            sb.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #1e1e1e; color: #e8e8e8; overflow: hidden; }");
            sb.AppendLine("        .container { width: 100vw; height: 100vh; display: flex; flex-direction: column; }");
            sb.AppendLine("        .header { padding: 20px; background: #2d2d30; border-bottom: 1px solid #3e3e42; }");
            sb.AppendLine("        .header h1 { font-size: 24px; font-weight: 300; color: #569cd6; margin-bottom: 8px; }");
            sb.AppendLine("        .header .subtitle { font-size: 14px; color: #9e9e9e; }");
            sb.AppendLine("        .controls { display: flex; gap: 10px; margin-top: 15px; }");
            sb.AppendLine("        .controls button { background: #0e639c; color: white; border: none; padding: 8px 16px; border-radius: 4px; cursor: pointer; font-size: 12px; transition: background 0.2s; }");
            sb.AppendLine("        .controls button:hover { background: #1177bb; }");
            sb.AppendLine("        .tree-container { flex: 1; position: relative; background: #1e1e1e; }");
            sb.AppendLine("        #tree { width: 100%; height: 100%; }");
            sb.AppendLine("        .node { cursor: pointer; transition: all 0.2s; }");
            sb.AppendLine("        .node circle { fill: #252526; stroke: #569cd6; stroke-width: 2px; transition: all 0.2s; }");
            sb.AppendLine("        .node:hover circle { stroke: #4ec9b0; stroke-width: 3px; r: 9; }");
            sb.AppendLine("        .node text { font: 13px 'Cascadia Code', 'Consolas', monospace; fill: #d4d4d4; text-shadow: 1px 1px 2px #000; pointer-events: none; }");
            sb.AppendLine("        .node.selected circle { stroke: #4ec9b0; stroke-width: 3px; fill: #2a2d2e; }");
            sb.AppendLine("        .link { fill: none; stroke: #464647; stroke-width: 1.5px; transition: stroke 0.2s; }");
            sb.AppendLine("        .link:hover { stroke: #4ec9b0; }");
            sb.AppendLine("        .tooltip { position: absolute; padding: 12px 16px; background: #252526; border: 1px solid #3c3c3c; border-radius: 6px; color: #d4d4d4; font: 13px 'Cascadia Code', 'Consolas', monospace; pointer-events: none; z-index: 1000; max-width: 400px; box-shadow: 0 4px 12px rgba(0,0,0,0.3); backdrop-filter: blur(4px); }");
            sb.AppendLine("        .tooltip h3 { color: #569cd6; margin-bottom: 8px; font-weight: 600; font-size: 14px; }");
            sb.AppendLine("        .tooltip .property { margin: 4px 0; color: #9cdcfe; }");
            sb.AppendLine("        .tooltip .value { color: #ce9178; margin-left: 8px; }");
            sb.AppendLine("        .tooltip .operator { color: #d7ba7d; }");
            sb.AppendLine("        .node-type-ProgramNode circle { stroke: #4CAF50; }");
            sb.AppendLine("        .node-type-TypeDeclarationNode circle { stroke: #2196F3; }");
            sb.AppendLine("        .node-type-VariableDeclarationNode circle { stroke: #FF9800; }");
            sb.AppendLine("        .node-type-RoutineDeclarationNode circle { stroke: #9C27B0; }");
            sb.AppendLine("        .node-type-PrimitiveTypeNode circle { stroke: #4EC9B0; }");
            sb.AppendLine("        .node-type-UserTypeNode circle { stroke: #C586C0; }");
            sb.AppendLine("        .node-type-ArrayTypeNode circle { stroke: #CE9178; }");
            sb.AppendLine("        .node-type-RecordTypeNode circle { stroke: #D7BA7D; }");
            sb.AppendLine("        .node-type-AssignmentNode circle { stroke: #F44747; }");
            sb.AppendLine("        .node-type-IfStatementNode circle { stroke: #00897B; }");
            sb.AppendLine("        .node-type-ForLoopNode circle { stroke: #7E57C2; }");
            sb.AppendLine("        .node-type-WhileLoopNode circle { stroke: #43A047; }");
            sb.AppendLine("        .node-type-PrintStatementNode circle { stroke: #FFB300; }");
            sb.AppendLine("        .node-type-ReturnStatementNode circle { stroke: #E57373; }");
            sb.AppendLine("        .node-type-IntegerLiteralNode circle { stroke: #B5CEA8; }");
            sb.AppendLine("        .node-type-RealLiteralNode circle { stroke: #B5CEA8; }");
            sb.AppendLine("        .node-type-BooleanLiteralNode circle { stroke: #B5CEA8; }");
            sb.AppendLine("        .node-type-IdentifierNode circle { stroke: #9CDCFE; }");
            sb.AppendLine("        .node-type-BinaryOperationNode circle { stroke: #D7BA7D; }");
            sb.AppendLine("        .node-type-UnaryOperationNode circle { stroke: #D7BA7D; }");
            sb.AppendLine("        .node-type-RecordAccessNode circle { stroke: #FF9800; }");
            sb.AppendLine("        .node-type-ArrayAccessNode circle { stroke: #CE9178; }");
            sb.AppendLine("        .node-type-RoutineCallNode circle { stroke: #C586C0; }");
            sb.AppendLine("        .node-type-RangeNode circle { stroke: #43A047; }");
            sb.AppendLine("        .node-type-ArrayInitializerNode circle { stroke: #CE9178; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class=\"container\">");
            sb.AppendLine("        <div class=\"header\">");
            sb.AppendLine($"            <h1>{title}</h1>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class=\"tree-container\">");
            sb.AppendLine("            <div id=\"tree\"></div>");
            sb.AppendLine("            <div class=\"tooltip\" id=\"tooltip\"></div>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <script>");
            sb.AppendLine("        const data = ");
            sb.AppendLine(GenerateTreeData(program));
            sb.AppendLine("        ;");
            sb.AppendLine(@"
        let selectedNode = null;
        const margin = { top: 20, right: 120, bottom: 20, left: 120 };
        const container = document.querySelector('.tree-container');
        const width = container.clientWidth - margin.left - margin.right;
        const height = container.clientHeight - margin.top - margin.bottom;

        const svg = d3.select('#tree')
            .append('svg')
            .attr('width', width + margin.left + margin.right)
            .attr('height', height + margin.top + margin.bottom);

        const g = svg.append('g')
            .attr('transform', `translate(${margin.left},${margin.top})`);

        const tooltip = d3.select('#tooltip');

        // Create tree layout with optimal spacing
        const treeLayout = d3.tree()
            .nodeSize([70, 220])
            .separation((a, b) => (a.parent == b.parent ? 1.2 : 1.8));

        const root = d3.hierarchy(data);
        treeLayout(root);

        // Links with smooth curves
        const link = g.selectAll('.link')
            .data(root.links())
            .enter().append('path')
            .attr('class', 'link')
            .attr('d', d3.linkHorizontal()
                .x(d => d.y)
                .y(d => d.x));

        // Nodes
        const node = g.selectAll('.node')
            .data(root.descendants())
            .enter().append('g')
            .attr('class', d => `node node-type-${d.data.type}`)
            .attr('transform', d => `translate(${d.y},${d.x})`)
            .on('mouseover', function(event, d) {
                showTooltip(event, d);
                d3.select(this).classed('selected', true);
            })
            .on('mouseout', function(event, d) {
                hideTooltip();
                d3.select(this).classed('selected', false);
            })
            .on('click', function(event, d) {
                event.stopPropagation();
                selectNode(d);
            });

        // Node circles
        node.append('circle')
            .attr('r', 7);

        // Node labels
        node.append('text')
            .attr('dy', 4)
            .attr('x', d => d.children ? -12 : 12)
            .attr('text-anchor', d => d.children ? 'end' : 'start')
            .style('font-weight', '500')
            .text(d => {
                let label = d.data.displayName || d.data.type || 'Node';
                // Shorten very long labels
                if (label.length > 20) {
                    label = label.substring(0, 18) + '..';
                }
                return label;
            });

        // Enhanced zoom behavior
        const zoom = d3.zoom()
            .scaleExtent([0.1, 3])
            .on('zoom', (event) => {
                g.attr('transform', event.transform);
                // Scale text size with zoom for better readability
                const scale = event.transform.k;
                node.selectAll('text')
                    .style('font-size', Math.max(10, 13 / scale) + 'px');
            });

        svg.call(zoom);

        // Tooltip functions
        function showTooltip(event, d) {
            const tooltipContent = generateTooltipContent(d);
            tooltip.html(tooltipContent)
                .style('opacity', 1)
                .style('left', (event.pageX + 15) + 'px')
                .style('top', (event.pageY - 15) + 'px');
        }

        function hideTooltip() {
            tooltip.style('opacity', 0);
        }

        function generateTooltipContent(d) {
            let content = `<h3>${d.data.type}</h3>`;
            
            if (d.data.properties) {
                d.data.properties.forEach(prop => {
                    content += `<div class='property'>${prop.name}: <span class='value'>${prop.value}</span></div>`;
                });
            }
            
            if (d.data.additionalInfo) {
                content += `<div class='property' style='margin-top: 8px; color: #9e9e9e;'>${d.data.additionalInfo}</div>`;
            }
            
            content += `<div class='property' style='margin-top: 8px; color: #9e9e9e;'>Depth: ${d.depth} | Children: ${d.children ? d.children.length : 0}</div>`;
            
            return content;
        }

        function selectNode(d) {
            if (selectedNode) {
                selectedNode.classed('selected', false);
            }
            selectedNode = d3.select(d3.select(d).node().parentNode);
            selectedNode.classed('selected', true);
        }



        // Handle window resize
        window.addEventListener('resize', function() {
            const newWidth = container.clientWidth - margin.left - margin.right;
            const newHeight = container.clientHeight - margin.top - margin.bottom;
            
            svg.attr('width', newWidth + margin.left + margin.right)
               .attr('height', newHeight + margin.top + margin.bottom);
            
            setTimeout(fitToScreen, 100);
        });

        // Auto-fit on load
        setTimeout(fitToScreen, 100);

        // Click outside to deselect
        svg.on('click', function(event) {
            if (event.target === this) {
                if (selectedNode) {
                    selectedNode.classed('selected', false);
                    selectedNode = null;
                }
                hideTooltip();
            }
        });
            ");
            sb.AppendLine("    </script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private string GenerateTreeData(ProgramNode program)
        {
            var root = new Dictionary<string, object>
            {
                ["name"] = "Program",
                ["type"] = "ProgramNode",
                ["displayName"] = "Program",
                ["properties"] = new List<object>
                {
                    new { name = "Declarations", value = program.Declarations.Count.ToString() }
                },
                ["children"] = new List<object>()
            };

            var children = (List<object>)root["children"];

            for (int i = 0; i < program.Declarations.Count; i++)
            {
                children.Add(ConvertDeclarationToTree(program.Declarations[i], i));
            }

            return JsonSerializer.Serialize(root, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }

        private Dictionary<string, object> ConvertDeclarationToTree(DeclarationNode decl, int index)
        {
            var node = new Dictionary<string, object>();

            switch (decl)
            {
                case TypeDeclarationNode typeDecl:
                    node["type"] = "TypeDeclarationNode";
                    node["displayName"] = $"Type[{index}]";
                    node["properties"] = new List<object>
                    {
                        new { name = "Name", value = $"\"{typeDecl.Name}\"" }
                    };
                    node["children"] = new List<object> { ConvertTypeToTree(typeDecl.Type) };
                    break;

                case VariableDeclarationNode varDecl:
                    node["type"] = "VariableDeclarationNode";
                    node["displayName"] = $"Var[{index}]";
                    node["properties"] = new List<object>
                    {
                        new { name = "Name", value = $"\"{varDecl.Name}\"" }
                    };
                    
                    var varChildren = new List<object> { ConvertTypeToTree(varDecl.Type) };
                    if (varDecl.InitialValue != null)
                    {
                        varChildren.Add(ConvertExpressionToTree(varDecl.InitialValue));
                    }
                    node["children"] = varChildren;
                    break;

                case RoutineDeclarationNode routineDecl:
                    node["type"] = "RoutineDeclarationNode";
                    node["displayName"] = $"Routine[{index}]";
                    node["properties"] = new List<object>
                    {
                        new { name = "Name", value = $"\"{routineDecl.Name}\"" },
                        new { name = "Parameters", value = routineDecl.Parameters.Count.ToString() }
                    };
                    
                    var routineChildren = new List<object>();
                    
                    // Parameters
                    if (routineDecl.Parameters.Count > 0)
                    {
                        var paramsNode = new Dictionary<string, object>
                        {
                            ["type"] = "ParameterList",
                            ["displayName"] = "Parameters",
                            ["properties"] = new List<object>
                            {
                                new { name = "Count", value = routineDecl.Parameters.Count.ToString() }
                            },
                            ["children"] = new List<object>()
                        };
                        
                        for (int i = 0; i < routineDecl.Parameters.Count; i++)
                        {
                            ((List<object>)paramsNode["children"]).Add(ConvertParameterToTree(routineDecl.Parameters[i], i));
                        }
                        routineChildren.Add(paramsNode);
                    }

                    // Return type
                    if (routineDecl.ReturnType != null)
                    {
                        routineChildren.Add(ConvertTypeToTree(routineDecl.ReturnType));
                    }

                    // Body
                    routineChildren.Add(ConvertBodyToTree(routineDecl.Body));
                    node["children"] = routineChildren;
                    break;
            }

            return node;
        }

        private Dictionary<string, object> ConvertParameterToTree(ParameterNode param, int index)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "Parameter",
                ["displayName"] = $"Param[{index}]",
                ["properties"] = new List<object>
                {
                    new { name = "Name", value = $"\"{param.Name}\"" }
                },
                ["children"] = new List<object> { ConvertTypeToTree(param.Type) }
            };
        }

        private Dictionary<string, object> ConvertTypeToTree(TypeNode type)
        {
            var node = new Dictionary<string, object>();

            switch (type)
            {
                case PrimitiveTypeNode prim:
                    node["type"] = "PrimitiveTypeNode";
                    node["displayName"] = "Primitive";
                    node["properties"] = new List<object>
                    {
                        new { name = "TypeName", value = $"\"{prim.TypeName}\"" }
                    };
                    break;

                case UserTypeNode user:
                    node["type"] = "UserTypeNode";
                    node["displayName"] = "UserType";
                    node["properties"] = new List<object>
                    {
                        new { name = "TypeName", value = $"\"{user.TypeName}\"" }
                    };
                    break;

                case ArrayTypeNode arr:
                    node["type"] = "ArrayTypeNode";
                    node["displayName"] = "ArrayType";
                    node["children"] = new List<object>
                    {
                        ConvertExpressionToTree(arr.Size),
                        ConvertTypeToTree(arr.ElementType)
                    };
                    break;

                case RecordTypeNode rec:
                    node["type"] = "RecordTypeNode";
                    node["displayName"] = "RecordType";
                    node["properties"] = new List<object>
                    {
                        new { name = "Fields", value = rec.Fields.Count.ToString() }
                    };
                    
                    var fieldChildren = new List<object>();
                    for (int i = 0; i < rec.Fields.Count; i++)
                    {
                        fieldChildren.Add(ConvertDeclarationToTree(rec.Fields[i], i));
                    }
                    node["children"] = fieldChildren;
                    break;
            }

            return node;
        }

        private Dictionary<string, object> ConvertBodyToTree(BodyNode body)
        {
            var node = new Dictionary<string, object>
            {
                ["type"] = "BodyNode",
                ["displayName"] = "Body",
                ["properties"] = new List<object>
                {
                    new { name = "Declarations", value = body.Declarations.Count.ToString() },
                    new { name = "Statements", value = body.Statements.Count.ToString() }
                },
                ["children"] = new List<object>()
            };

            var children = (List<object>)node["children"];

            // Declarations
            if (body.Declarations.Count > 0)
            {
                var declsNode = new Dictionary<string, object>
                {
                    ["type"] = "DeclarationList",
                    ["displayName"] = "Declarations",
                    ["properties"] = new List<object>
                    {
                        new { name = "Count", value = body.Declarations.Count.ToString() }
                    },
                    ["children"] = new List<object>()
                };
                
                for (int i = 0; i < body.Declarations.Count; i++)
                {
                    ((List<object>)declsNode["children"]).Add(ConvertDeclarationToTree(body.Declarations[i], i));
                }
                children.Add(declsNode);
            }

            // Statements
            if (body.Statements.Count > 0)
            {
                var stmtsNode = new Dictionary<string, object>
                {
                    ["type"] = "StatementList",
                    ["displayName"] = "Statements",
                    ["properties"] = new List<object>
                    {
                        new { name = "Count", value = body.Statements.Count.ToString() }
                    },
                    ["children"] = new List<object>()
                };
                
                for (int i = 0; i < body.Statements.Count; i++)
                {
                    ((List<object>)stmtsNode["children"]).Add(ConvertStatementToTree(body.Statements[i], i));
                }
                children.Add(stmtsNode);
            }

            return node;
        }

        private Dictionary<string, object> ConvertStatementToTree(StatementNode stmt, int index)
        {
            var node = new Dictionary<string, object>
            {
                ["type"] = stmt.GetType().Name,
                ["displayName"] = $"{stmt.GetType().Name.Replace("Node", "")}[{index}]"
            };

            var children = new List<object>();

            switch (stmt)
            {
                case AssignmentNode assign:
                    node["properties"] = new List<object> { new { name = "Type", value = "Assignment" } };
                    children.Add(ConvertExpressionToTree(assign.Target));
                    children.Add(ConvertExpressionToTree(assign.Value));
                    break;

                case IfStatementNode ifStmt:
                    node["properties"] = new List<object>
                    {
                        new { name = "Then", value = ifStmt.ThenBody.Count.ToString() },
                        new { name = "Else", value = ifStmt.ElseBody.Count.ToString() }
                    };
                    children.Add(ConvertExpressionToTree(ifStmt.Condition));
                    
                    var thenNode = new Dictionary<string, object>
                    {
                        ["type"] = "ThenBody",
                        ["displayName"] = "Then",
                        ["properties"] = new List<object> { new { name = "Statements", value = ifStmt.ThenBody.Count.ToString() } },
                        ["children"] = new List<object>()
                    };
                    foreach (var thenStmt in ifStmt.ThenBody)
                    {
                        ((List<object>)thenNode["children"]).Add(ConvertStatementToTree(thenStmt, 0));
                    }
                    children.Add(thenNode);

                    if (ifStmt.ElseBody.Count > 0)
                    {
                        var elseNode = new Dictionary<string, object>
                        {
                            ["type"] = "ElseBody",
                            ["displayName"] = "Else",
                            ["properties"] = new List<object> { new { name = "Statements", value = ifStmt.ElseBody.Count.ToString() } },
                            ["children"] = new List<object>()
                        };
                        foreach (var elseStmt in ifStmt.ElseBody)
                        {
                            ((List<object>)elseNode["children"]).Add(ConvertStatementToTree(elseStmt, 0));
                        }
                        children.Add(elseNode);
                    }
                    break;

                case ForLoopNode forLoop:
                    node["properties"] = new List<object>
                    {
                        new { name = "Variable", value = $"\"{forLoop.Variable}\"" },
                        new { name = "Reverse", value = forLoop.IsReverse.ToString() },
                        new { name = "Body", value = forLoop.Body.Count.ToString() }
                    };
                    children.Add(ConvertExpressionToTree(forLoop.Range));
                    
                    var forBodyNode = new Dictionary<string, object>
                    {
                        ["type"] = "ForBody",
                        ["displayName"] = "Body",
                        ["properties"] = new List<object> { new { name = "Statements", value = forLoop.Body.Count.ToString() } },
                        ["children"] = new List<object>()
                    };
                    foreach (var forStmt in forLoop.Body)
                    {
                        ((List<object>)forBodyNode["children"]).Add(ConvertStatementToTree(forStmt, 0));
                    }
                    children.Add(forBodyNode);
                    break;

                case PrintStatementNode printStmt:
                    node["properties"] = new List<object> { new { name = "Type", value = "Print" } };
                    children.Add(ConvertExpressionToTree(printStmt.Expression));
                    break;

                default:
                    node["properties"] = new List<object> { new { name = "Type", value = "Statement" } };
                    break;
            }

            if (children.Count > 0)
            {
                node["children"] = children;
            }

            return node;
        }

        private Dictionary<string, object> ConvertExpressionToTree(ExpressionNode expr)
        {
            var node = new Dictionary<string, object>
            {
                ["type"] = expr.GetType().Name,
                ["displayName"] = expr.GetType().Name.Replace("Node", "")
            };

            var children = new List<object>();

            switch (expr)
            {
                case IntegerLiteralNode intLit:
                    node["properties"] = new List<object> { new { name = "Value", value = intLit.Value.ToString() } };
                    break;

                case RealLiteralNode realLit:
                    node["properties"] = new List<object> { new { name = "Value", value = realLit.Value.ToString() } };
                    break;

                case BooleanLiteralNode boolLit:
                    node["properties"] = new List<object> { new { name = "Value", value = boolLit.Value.ToString() } };
                    break;

                case IdentifierNode id:
                    node["properties"] = new List<object> { new { name = "Name", value = $"\"{id.Name}\"" } };
                    break;

                case BinaryOperationNode binOp:
                    node["properties"] = new List<object> { new { name = "Operator", value = $"\"{binOp.Operator}\"" } };
                    children.Add(ConvertExpressionToTree(binOp.Left));
                    children.Add(ConvertExpressionToTree(binOp.Right));
                    break;

                case RecordAccessNode recAccess:
                    node["properties"] = new List<object> { new { name = "Field", value = $"\"{recAccess.FieldName}\"" } };
                    children.Add(ConvertExpressionToTree(recAccess.Record));
                    break;

                case ArrayAccessNode arrAccess:
                    node["properties"] = new List<object> { new { name = "Type", value = "Array Access" } };
                    children.Add(ConvertExpressionToTree(arrAccess.Array));
                    children.Add(ConvertExpressionToTree(arrAccess.Index));
                    break;

                case RoutineCallNode call:
                    node["properties"] = new List<object>
                    {
                        new { name = "Routine", value = $"\"{call.RoutineName}\"" },
                        new { name = "Arguments", value = call.Arguments.Count.ToString() }
                    };
                    foreach (var arg in call.Arguments)
                    {
                        children.Add(ConvertExpressionToTree(arg));
                    }
                    break;

                case ArrayInitializerNode arrInit:
                    node["properties"] = new List<object> { new { name = "Elements", value = arrInit.Elements.Count.ToString() } };
                    foreach (var element in arrInit.Elements)
                    {
                        children.Add(ConvertExpressionToTree(element));
                    }
                    break;

                case RangeNode range:
                    children.Add(new Dictionary<string, object> 
                    { 
                        ["type"] = "Start",
                        ["children"] = new List<object> { ConvertExpressionToTree(range.Start) }
                    });
                    children.Add(new Dictionary<string, object> 
                    { 
                        ["type"] = "Property",
                        ["children"] = new List<object> { ConvertExpressionToTree(range.End) }
                    });
                    break;

                default:
                    node["properties"] = new List<object> { new { name = "Type", value = "Expression" } };
                    break;
            }

            if (children.Count > 0)
            {
                node["children"] = children;
            }

            return node;
        }
    }
}