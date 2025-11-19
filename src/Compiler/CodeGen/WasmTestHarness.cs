namespace Compiler.CodeGen;

public class FunctionInfo
{
    public string Name { get; set; } = "";
    public List<(string Name, string Type)> Parameters { get; set; } = new();
    public string? ReturnType { get; set; }
}

public class WasmTestHarness
{
    public void Generate(string outputDirectory, string baseName, List<FunctionInfo> functions)
    {
        string jsPath = Path.Combine(outputDirectory, $"{baseName}_test.js");
        File.WriteAllText(jsPath, GenerateNodeScript(baseName, functions));
    }

    private string GenerateNodeScript(string baseName, List<FunctionInfo> functions)
    {
        var functionsList = string.Join(",\n", functions.Select(f => 
            $"  {{name: '{f.Name}', params: [{string.Join(", ", f.Parameters.Select(p => $"{{name: '{p.Name}', type: '{p.Type}'}}"))}], returnType: {(f.ReturnType != null ? $"'{f.ReturnType}'" : "null")}}}"
        ));

        return $@"#!/usr/bin/env node
const fs = require('fs');
const path = require('path');

// WASM imports
const importObject = {{
    env: {{
        printInt: (value) => {{
            console.log(value);
        }},
        printReal: (value) => {{
            console.log(value.toFixed(6));
        }},
        printBool: (value) => {{
            console.log(value ? 'true' : 'false');
        }}
    }}
}};

const functions = [
{functionsList}
];

async function loadWasm() {{
    try {{
        const wasmPath = path.join(__dirname, '{baseName}.wasm');
        const wasmBuffer = fs.readFileSync(wasmPath);
        const wasmModule = await WebAssembly.compile(wasmBuffer);
        const instance = await WebAssembly.instantiate(wasmModule, importObject);
        return instance.exports;
    }} catch (e) {{
        console.error('Error loading WASM:', e.message);
        process.exit(1);
    }}
}}

function parseValue(value, type) {{
    const normalizedType = type.toLowerCase();
    if (normalizedType === 'integer') {{
        return parseInt(value);
    }} else if (normalizedType === 'real') {{
        return parseFloat(value);
    }} else if (normalizedType === 'boolean') {{
        return value === 'true' || value === '1';
    }}
    return value;
}}

function printHelp() {{
    console.log('Usage: node {baseName}_test.js <function_name> [args...]');
    console.log('');
    console.log('Available functions:');
    functions.forEach(func => {{
        const params = func.params.map(p => `${{p.name}}:${{p.type}}`).join(', ');
        const ret = func.returnType ? `: ${{func.returnType}}` : '';
        console.log(`  ${{func.name}}(${{params}})${{ret}}`);
    }});
    console.log('');
    console.log('Examples:');
    console.log('  node {baseName}_test.js main');
    console.log('  node {baseName}_test.js test 10 5');
}}

async function main() {{
    const args = process.argv.slice(2);
    
    if (args.length === 0 || args[0] === '--help' || args[0] === '-h') {{
        printHelp();
        return;
    }}
    
    const functionName = args[0];
    const functionArgs = args.slice(1);
    
    const funcInfo = functions.find(f => f.name === functionName);
    if (!funcInfo) {{
        console.error(`Error: Function '${{functionName}}' not found`);
        printHelp();
        process.exit(1);
    }}
    
    if (functionArgs.length !== funcInfo.params.length) {{
        console.error(`Error: Function '${{functionName}}' expects ${{funcInfo.params.length}} arguments, got ${{functionArgs.length}}`);
        const params = funcInfo.params.map(p => `${{p.name}}:${{p.type}}`).join(', ');
        console.error(`Expected: ${{functionName}}(${{params}})`);
        process.exit(1);
    }}
    
    const exports = await loadWasm();
    
    console.log(`\n=== Executing ${{functionName}}(${{functionArgs.join(', ')}}) ===\n`);
    
    try {{
        const parsedArgs = functionArgs.map((arg, i) => parseValue(arg, funcInfo.params[i].type));
        const result = exports[functionName](...parsedArgs);
        
        if (result !== undefined) {{
            console.log(`\n=== Return value: ${{result}} ===`);
        }} else {{
            console.log(`\n=== Execution completed ===`);
        }}
    }} catch (e) {{
        console.error(`\nError executing function: ${{e.message}}`);
        process.exit(1);
    }}
}}

main().catch(e => {{
    console.error('Fatal error:', e);
    process.exit(1);
}});
";
    }

}

