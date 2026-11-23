#!/usr/bin/env python3
"""Generate the long-tail Backend API / Platform API surface of the .NET SDK
from the public OpenAPI document.

Usage (from sdks/dotnet):

    python3 scripts/gen_catalog.py                  # from scripts/openapi.snapshot.json
    python3 scripts/gen_catalog.py --spec FILE      # a local copy of the spec
    python3 scripts/gen_catalog.py --freeze         # also (re)write scripts/catalog_pins.json

CI regenerates from the checked-in snapshot and fails on any diff. To pick up
API changes: copy a fresh sdks/rust/scripts/openapi.snapshot.json to scripts/,
regenerate, review `git diff` (additions are fine).

Which operations: every BAPI + PLATFORM operation whose verb AND path no
hand-written source already uses (every `HttpVerb.X` call with its /v1 path,
under src/ outside Generated/, Primitives/ and Rp/). An operation sharing a
path with a hand-written one of another verb is generated. "generate_anyway"
("POST /v1/x") in catalog_pins.json forces one.

Writes:

  src/Atlas/Generated/Catalog.cs        AtlasCatalog + one resource class per namespace
                                        (client.Catalog.<Namespace>.<Op>Async)
  src/Atlas/Generated/CatalogModels.cs  records for every request, response and query shape
  src/Atlas/AtlasClient.Catalog.cs      the `Catalog` accessor on AtlasClient
  tests/Atlas.Tests/api-operations.txt  the generated operations are appended (ApiCoverageTests)

Published signatures are pinned: scripts/catalog_pins.json maps each operation
to its method name ("methods") and each shape to its type name ("types"), so a
regeneration never renames something callers already use. New operations and
shapes need no pin. A pin the spec no longer reaches fails the run.

Type mapping: object -> sealed record (init-only, [JsonPropertyName] wire names,
unknown keys ignored); optional/nullable -> T?; enum/const -> string (values in
the doc comment, so a new server value never breaks parsing); array ->
List<T>; map -> Dictionary<string, T>; multi-type / open schema -> object?
(JsonElement on read). Request-side records are all-optional (nulls are dropped
on write); response-side required scalars are non-null with a default.
"""
import json, re, os, sys, glob, hashlib

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
SRC = os.path.join(ROOT, 'src', 'Atlas')
GEN_DIR = os.path.join(SRC, 'Generated')
SPEC = os.path.join(ROOT, 'scripts', 'openapi.snapshot.json')
if '--spec' in sys.argv:
    SPEC = sys.argv[sys.argv.index('--spec') + 1]
FREEZE = '--freeze' in sys.argv
PRUNE = '--prune' in sys.argv  # drop pins the spec no longer reaches (with --freeze)
PINS_FILE = os.path.join(HERE, 'catalog_pins.json')
OPS_TXT = os.path.join(ROOT, 'tests', 'Atlas.Tests', 'api-operations.txt')

d = json.load(open(SPEC))
SCHEMAS = d['components']['schemas']
PINS = json.load(open(PINS_FILE)) if os.path.exists(PINS_FILE) else {}
PINS.setdefault('methods', {})
PINS.setdefault('types', {})
PINS.setdefault('generate_anyway', [])
USED_PINS = set()

# ── which operations (mirrors scripts/sdk-field-drift.mjs path matching) ─────
def canon(p):
    return re.sub(r'\{[^}]*\}', '{}', p).rstrip('/') or '/'

def paths_in(text):
    """Every /v1 path literal in the text, in order, with interpolation and concatenation collapsed to {}."""
    t = re.sub(r'(["\'`])\s*(?:\+|\.|<>)\s*[^"\'`\n]{1,120}?\s*(?:\+|\.|<>)\s*\1', '{}', text)
    t = re.sub(r'(/v1/[\w\-./]*/)(["\'`])\s*(?:\+|\.|<>)\s*[\w$.()\[\]]+', r'\1{}\2', t)
    t = re.sub(r'(?:#|\$)?\{\{?[^{}"\'`\n]{0,120}\}?\}', '{}', t)
    out = []
    for m in re.finditer(r'/v1/[A-Za-z0-9_\-./{}:~]*', t):
        p = m.group(0).rstrip('.')
        if p.endswith('/'):
            p += '{}'
        out.append(canon(p))
    return out

def sdk_paths(text):
    return set(paths_in(text))

# Hand-written (VERB, path) pairs: every `HttpVerb.X` in the sources, with the first /v1 path literal that follows it
# (`Req<T>(HttpVerb.Get, $"/v1/x/{id}")` and `new RequestOptions { Method = HttpVerb.Get, Path = "/v1/x" }` alike).
# shortcut: a verb whose path comes from a variable with no /v1 literal nearby is not seen, so that operation is also generated
# (a harmless duplicate, never a gap).
VERB_RE = re.compile(r'HttpVerb\.(Get|Post|Put|Patch|Delete)\b')
handwritten = set()
hand_paths = set()
for f in glob.glob(os.path.join(SRC, '**', '*.cs'), recursive=True):
    if any(os.sep + d + os.sep in f for d in ('Generated', 'Primitives', 'Rp')):
        continue  # Primitives and Rp call the API directly, not through client methods
    text = open(f, encoding='utf-8').read()
    hand_paths |= sdk_paths(text)
    for m in VERB_RE.finditer(text):
        snippet = text[m.end():m.end() + 240].split(';')[0]
        found = paths_in(snippet)
        if found:
            handwritten.add((m.group(1).upper(), found[0]))

METHODS = ('get', 'post', 'put', 'patch', 'delete')
ops = []  # (method, path, op)
shape_ops = []  # hand-written operations: only their wire shapes are generated
for p in sorted(d['paths']):
    for m in METHODS:
        o = d['paths'][p].get(m)
        if not o or o.get('tags', [''])[0] not in ('BAPI', 'PLATFORM'):
            continue
        if (m.upper(), canon(p)) in handwritten and f'{m.upper()} {p}' not in PINS['generate_anyway']:
            shape_ops.append((m, p, o))
            continue
        ops.append((m, p, o))

# ── names ────────────────────────────────────────────────────────────────────
CS_KW = set('''abstract as base bool break byte case catch char checked class const continue decimal default delegate do
double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal
is lock long namespace new null object operator out override params private protected public readonly ref return sbyte
sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort
using virtual void volatile while'''.split())
OBJECT_MEMBERS = {'Equals', 'GetHashCode', 'GetType', 'ToString', 'MemberwiseClone', 'Finalize', 'EqualityContract',
                  'Clone', 'PrintMembers', 'ToQuery', 'Cleared', 'Plus', 'Deconstruct', 'ReferenceEquals'}

def words(s):
    s = re.sub(r'([a-z0-9])([A-Z])', r'\1 \2', s)
    return [w for w in re.split(r'[^A-Za-z0-9]+', s) if w]

def pascal(s):
    out = ''.join(w[:1].upper() + w[1:] for w in words(s))
    if not out:
        out = 'Value'
    if out[0].isdigit():
        out = 'N' + out
    return out

def camel(s):
    p = pascal(s)
    c = p[:1].lower() + p[1:]
    return '@' + c if c in CS_KW else c

def singular(w):
    if w.endswith('ies') and len(w) > 4:
        return w[:-3] + 'y'
    if w.endswith('sses') or w.endswith('uses'):
        return w[:-2]
    if w.endswith('s') and not w.endswith('ss') and not w.endswith('us') and len(w) > 3:
        return w[:-1]
    return w

existing_types = set()
for f in glob.glob(os.path.join(ROOT, 'src', '**', '*.cs'), recursive=True):
    if os.sep + 'Generated' + os.sep in f:
        continue
    existing_types |= set(re.findall(r'\b(?:class|record|struct|enum|interface)\s+(\w+)', open(f, encoding='utf-8').read()))
taken_types = set(existing_types)

def claim_type(key, wanted):
    """A unique, pinned type name for a shape."""
    if key in PINS['types']:
        name = PINS['types'][key]
        USED_PINS.add(('types', key))
        taken_types.add(name)
        return name
    name, i = wanted, 2
    if name in taken_types:
        name = wanted + 'Model'
    while name in taken_types:
        name = f'{wanted}{i}'
        i += 1
    taken_types.add(name)
    PINS_OUT['types'][key] = name
    return name

PINS_OUT = {'methods': {}, 'types': {}}
taken_types |= set(PINS['types'].values())

# ── schema -> C# type ────────────────────────────────────────────────────────
def deref(s):
    seen = 0
    while isinstance(s, dict) and '$ref' in s and seen < 20:
        s = SCHEMAS[s['$ref'].split('/')[-1]]
        seen += 1
    return s

def merged(s):
    s = deref(s)
    props, req, addl = {}, set(), s.get('additionalProperties')
    for part in s.get('allOf', []) or []:
        p2, r2, a2 = merged(part)
        props.update(p2)
        req |= r2
        if a2 is not None:
            addl = a2
    props.update(s.get('properties') or {})
    req |= set(s.get('required') or [])
    return props, req, addl

def types_of(s):
    t = s.get('type')
    if t is None:
        return []
    return t if isinstance(t, list) else [t]

def esc(text):
    t = (text or '').replace('\r', ' ').replace('\n', ' ').strip()
    return t.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')

def doc(text, indent, extra=None):
    t = esc(text)
    if extra:
        t = (t + ' ' + extra).strip()
    if not t:
        return ''
    return f'{indent}/// <summary>{t}</summary>\n'

MODELS = []          # emitted record source, in order
NAME_SHAPE = {}      # type name -> shape hash, one name must mean one shape
SHAPES = {}          # (canonical json, mode) -> name
BUILDING = set()

class Ctx:
    pass

def enum_note(s):
    vals = [v for v in (s.get('enum') or []) if v is not None]
    if not vals and 'const' in s:
        vals = [s['const']]
    return ('One of: ' + ', '.join(str(v) for v in vals) + '.') if vals else None

def cs_type(s, hint, key, mode):
    """-> (type without ?, kind) where kind in scalar/ref/list/dict/open."""
    s0 = s
    if isinstance(s, dict) and '$ref' in s:
        name = s['$ref'].split('/')[-1]
        target = SCHEMAS[name]
        if struct_like(target):
            return emit_record(target, f'schema:{name}:{mode}', pascal(name) + ('Input' if mode == 'request' else ''), mode), 'ref'
        return cs_type(target, pascal(name), f'schema:{name}', mode)
    s = deref(s)
    if not isinstance(s, dict) or not s:
        return 'object', 'open'
    for k in ('anyOf', 'oneOf'):
        if k in s:
            alts = [a for a in s[k] if not (isinstance(a, dict) and types_of(a) == ['null'])]
            if len(alts) == 1:
                return cs_type(alts[0], hint, key, mode)
            # no unions in C#: the variants still become records, the member stays untyped (object?)
            for i, alt in enumerate(alts):
                if struct_like(alt):
                    cs_type(alt, hint + 'Variant' + str(i + 1), f'{key}|{i}', mode)
            return 'object', 'open'
    if 'allOf' in s and not s.get('properties'):
        if struct_like(s):
            return emit_record(s, key, hint, mode), 'ref'
        parts = s['allOf']
        if len(parts) == 1:
            return cs_type(parts[0], hint, key, mode)
        return 'object', 'open'
    ts = [t for t in types_of(s) if t != 'null']
    if not ts and 'const' in s:
        return 'string', 'scalar'
    if not ts and 'enum' in s:
        return 'string', 'scalar'
    if len(ts) != 1:
        return 'object', 'open'
    t = ts[0]
    if t == 'string':
        return 'string', 'scalar'
    if t == 'integer':
        return 'long', 'scalar'
    if t == 'number':
        return 'double', 'scalar'
    if t == 'boolean':
        return 'bool', 'scalar'
    if t == 'array':
        it = s.get('items')
        if not it:
            return 'List<object?>', 'list'
        inner, kind = cs_type(it, hint + 'Item', key + '[]', mode)
        return f'List<{nullable(inner, kind, it)}>', 'list'
    if t == 'object':
        if struct_like(s):
            return emit_record(s, key, hint, mode), 'ref'
        addl = s.get('additionalProperties')
        if isinstance(addl, dict) and addl:
            inner, kind = cs_type(addl, hint + 'Value', key + '{}', mode)
            return f'Dictionary<string, {nullable(inner, kind, addl)}>', 'dict'
        return 'Dictionary<string, object?>', 'dict'
    return 'object', 'open'

def is_nullable_schema(s):
    s = deref(s) if isinstance(s, dict) else s
    if not isinstance(s, dict):
        return False
    if 'null' in types_of(s):
        return True
    for k in ('anyOf', 'oneOf'):
        if any(isinstance(a, dict) and types_of(a) == ['null'] for a in s.get(k, [])):
            return True
    return False

def nullable(t, kind, schema=None):
    # collection items: null on the wire only where the schema says so (open values always may be)
    if t == 'object':
        return 'object?'
    if kind == 'scalar' and schema is not None and is_nullable_schema(schema):
        return t + '?'
    return t

def struct_like(s):
    s = deref(s)
    if not isinstance(s, dict):
        return False
    if s.get('properties'):
        return True
    return any(struct_like(p) for p in s.get('allOf', []) or [])

def prop_name(wire, owner, used):
    n = pascal(wire)
    if wire == 'object':
        n = 'ObjectType'
    if n == owner or n in OBJECT_MEMBERS:
        n += 'Value'
    base, i = n, 2
    while n in used:
        n = f'{base}{i}'
        i += 1
    used.add(n)
    return n

def default_for(t):
    if t == 'string':
        return ' = "";'
    if t.startswith('List<') or t.startswith('Dictionary<'):
        return ' = new();'
    return ''

def emit_record(s, key, hint, mode):
    props, req, _ = merged(s)
    canonical = json.dumps([sorted(props.items(), key=lambda kv: kv[0]), sorted(req)], sort_keys=True)
    shape = (canonical, mode)
    # a pinned name HEAD already published must be emitted even when a new op reached this shape first
    pin = PINS['types'].get(key)
    if shape in SHAPES and pin and pin in NAME_SHAPE and NAME_SHAPE[pin] == hashlib.sha256(json.dumps(shape).encode()).hexdigest():
        return pin  # this key's own pinned name wins, so a response and its page item agree
    if shape in SHAPES and not (pin and pin not in NAME_SHAPE):
        return SHAPES[shape]
    name = claim_type(key, hint)
    h = hashlib.sha256(json.dumps(shape).encode()).hexdigest()
    if name in NAME_SHAPE and NAME_SHAPE[name] != h:
        # a union variant's key carries no mode, so its request and response shapes share one pinned name: split them
        name = claim_type(f'{key}@{mode}', hint + ('Input' if mode == 'request' else ''))
        if name in NAME_SHAPE and NAME_SHAPE[name] != h:
            sys.exit(f'type name {name} maps to two different shapes')
    NAME_SHAPE[name] = h
    if pin and name == pin: SHAPES[shape] = name  # later ops with this shape follow the published name
    else: SHAPES.setdefault(shape, name)
    used, lines, clears = set(), [], []
    for wire, ps in props.items():
        pn = prop_name(wire, name, used)
        t, kind = cs_type(ps, name + pascal(wire), f'{key}.{wire}', mode)
        required = wire in req and mode == 'response' and not is_nullable_schema(ps)
        d0 = deref(ps) if isinstance(ps, dict) else {}
        note = enum_note(d0) if isinstance(d0, dict) else None
        text = (ps.get('description') if isinstance(ps, dict) else None) or (d0.get('description') if isinstance(d0, dict) else None)
        if wire == 'object' and isinstance(d0, dict) and 'const' in d0 and not required:
            required = mode == 'response'
        if required and (kind in ('scalar', 'list', 'dict')):
            decl = f'public {t} {pn} {{ get; init; }}{default_for(t)}'
        else:
            tt = t if t.endswith('?') else t + '?'
            decl = f'public {tt} {pn} {{ get; init; }}'
        lines.append(doc(text, '        ', note) + f'        [JsonPropertyName("{wire}")] {decl}\n')
        if mode == 'request' and is_nullable_schema(ps):
            clears.append((wire, pn))
    # request records send null (the API's "clear") only through Clear<Property>(); unset stays omitted
    for wire, pn in clears:
        cn = 'Clear' + pn
        if cn in used:
            continue
        used.add(cn)
        lines.append(f'        /// <summary>Send <c>null</c> for <c>{wire}</c>, which clears it.</summary>\n        public {name} {cn}() => this with {{ Cleared = Plus("{wire}") }};\n')
    desc = deref(s).get('description') if isinstance(deref(s), dict) else None
    body = ''.join(lines)
    base = ' : ClearableBody' if mode == 'request' else ''
    MODELS.append(doc(desc or f'A {name} object.', '    ') + f'    public sealed record {name}{base}\n    {{\n{body}    }}\n')
    return name

def schema_of(o, where):
    if where == 'request':
        c = (o.get('requestBody') or {}).get('content', {}).get('application/json')
        return None if c is None else (c.get('schema') or {})
    for code in sorted(o.get('responses', {})):
        if not code.startswith('2'):
            continue
        content = o['responses'][code].get('content') or {}
        j = content.get('application/json')
        if j and j.get('schema') is not None:
            return j['schema']
        return None
    return None

def response_empty(o):
    for code in sorted(o.get('responses', {})):
        if code.startswith('2') and not (o['responses'][code].get('content') or {}):
            return True
        if code.startswith('2'):
            return False
    return True

# ── namespaces and method names ──────────────────────────────────────────────
def ns_of(p, tag):
    segs = [s for s in p.split('/') if s][1:]
    if not segs:
        return 'Root', []
    if segs[0] == 'platform' and len(segs) > 1:
        return 'Platform' + pascal(segs[1]), segs[2:]
    return pascal(segs[0]), segs[1:]

get_paths = {p for p in d['paths'] if 'get' in d['paths'][p]}

def base_name(m, p, rest):
    lits = [s for s in rest if not s.startswith('{')]
    ends_param = bool(rest) and rest[-1].startswith('{')
    last = lits[-1] if lits else ''
    coll = (not ends_param) and bool(rest) and p in get_paths
    if not rest:
        return {'get': 'List', 'post': 'Create', 'put': 'Set', 'patch': 'Update', 'delete': 'Delete'}[m]
    if ends_param and not lits:
        return {'get': 'Get', 'post': 'Create', 'put': 'Replace', 'patch': 'Update', 'delete': 'Delete'}[m]
    if ends_param:
        sing = ''.join(pascal(x) for x in lits[:-1]) + pascal(singular(last))
        return {'get': 'Get', 'post': 'Create', 'put': 'Replace', 'patch': 'Update', 'delete': 'Delete'}[m] + sing
    head = ''.join(pascal(x) for x in lits[:-1])
    if m == 'get':
        return ('List' if coll and is_plural(last) else 'Get') + head + pascal(last)
    if m == 'post':
        return 'Create' + head + pascal(singular(last)) if coll and is_plural(last) else head + pascal(last)
    if m == 'put':
        return 'Set' + head + pascal(last)
    if m == 'patch':
        return 'Update' + head + pascal(last)
    return 'Delete' + head + pascal(last)

def is_plural(w):
    return w.endswith('s') and not w.endswith('ss') and not w.endswith('us')

def method_name(m, p, rest, taken):
    key = f'{m.upper()} {p}'
    if key in PINS['methods']:
        USED_PINS.add(('methods', key))
        n = PINS['methods'][key]
        taken.add(n)
        return n
    n = base_name(m, p, rest)
    params = [pascal(x.strip('{}')) for x in rest if x.startswith('{')]
    cand = [n, n + 'By' + ''.join(params[-1:]), n + 'By' + ''.join(params)]
    for c in cand:
        if c and c + 'Async' not in taken :
            taken.add(c + 'Async')
            PINS_OUT['methods'][key] = c + 'Async'
            return c + 'Async'
    i = 2
    while f'{n}{i}Async' in taken:
        i += 1
    taken.add(f'{n}{i}Async')
    PINS_OUT['methods'][key] = f'{n}{i}Async'
    return f'{n}{i}Async'

# ── per-operation emission ───────────────────────────────────────────────────
def op_params(p, o):
    path, query, idem = [], [], False
    for q in o.get('parameters', []) + d['paths'][p].get('parameters', []):
        if '$ref' in q:
            continue
        if q['in'] == 'path':
            path.append(q)
        elif q['in'] == 'query':
            query.append(q)
        elif q['in'] == 'header' and q['name'].lower() == 'idempotency-key':
            idem = True
    return path, query, idem

resources = {}  # ns -> {'platform': bool, 'methods': [src], 'doc': ...}
sample_calls = []  # (ns, method name, http method, path)
op_lines = []

def emit_query(cls, mname, query, key):
    name = claim_type(key, cls[:-len('Operations')] + mname[:-len('Async')] + 'Query')
    used, props, yields = set(), [], []
    for q in query:
        sch = q.get('schema', {})
        t, kind = cs_type(sch, name, key + q['name'], 'request')
        if kind in ('ref', 'dict', 'open'):
            t = 'string'
        elif t.startswith('List<'):
            pass
        pn = prop_name(q['name'], name, used)
        if q['name'] == 'object':
            pn = 'ObjectType'
        tt = t if t.endswith('?') else t + '?'
        props.append(doc(q.get('description'), '        ', enum_note(sch)) + f'        public {tt} {pn} {{ get; init; }}\n')
        yields.append(f'            yield return new KeyValuePair<string, object?>("{q["name"]}", {pn});\n')
    MODELS.append(
        f'    /// <summary>Query parameters of {esc(mname[:-5])}. Unset values are not sent.</summary>\n'
        f'    public sealed record {name}\n    {{\n{"".join(props)}\n'
        f'        /// <summary>Flatten to query key/value pairs.</summary>\n'
        f'        public IEnumerable<KeyValuePair<string, object?>> ToQuery()\n        {{\n{"".join(yields)}        }}\n    }}\n')
    return name

for m, p, o, emit in [(*x, True) for x in ops] + [(*x, False) for x in shape_ops]:
    tag = o['tags'][0]
    ns, rest = ns_of(p, tag)
    if tag == 'PLATFORM' and not ns.startswith('Platform'):
        ns += 'Platform'
    cls = ns + 'Operations'
    if emit:
        r = resources.setdefault(ns, {'platform': tag == 'PLATFORM', 'methods': [], 'names': set(), 'cls': cls})
        mname = method_name(m, p, rest, r['names'])
    else:
        mname = base_name(m, p, rest) + 'Async'
    path_params, query, idem = op_params(p, o)
    used_args = {'body', 'query', 'idempotencyKey', 'cancellationToken'}
    args, path_expr = [], p
    for pn_ in re.findall(r'\{([^}]+)\}', p):
        q = {'name': pn_}
        a = camel(q['name'])
        while a.strip('@') in used_args:
            a = a + 'Value'
        used_args.add(a.strip('@'))
        args.append(f'string {a}')
        path_expr = path_expr.replace('{' + q['name'] + '}', '{Enc(' + a + ')}')
    # response
    rs = schema_of(o, 'response')
    empty = response_empty(o) or rs is None
    page_item = None
    if empty:
        ret = 'Task'
        rtype = None
    else:
        rt, rkind = cs_type(rs, f'{cls[:-10]}{mname[:-5]}Response', f'op:{m.upper()} {p}:response', 'response')
        rtype = rt
        ret = f'Task<{rt}>'
        if rkind == 'ref':
            rp, _, _ = merged(rs)
            if {'data', 'has_more', 'next_cursor'} <= set(rp) and types_of(deref(rp['data'])) == ['array'] and deref(rp['data']).get('items') and struct_like(deref(rp['data'])['items']):
                page_item = cs_type(deref(rp['data'])['items'], rt + 'Item', f'op:{m.upper()} {p}:response.data[]', 'response')[0]
                page_item = page_item.rstrip('?')
    # body
    bs = schema_of(o, 'request')
    body_arg = None
    if bs is not None:
        bt, bkind = cs_type(bs, f'{cls[:-10]}{mname[:-5]}Body', f'op:{m.upper()} {p}:body', 'request')
        bt = bt.rstrip('?')
        reqd = bool(o['requestBody'].get('required'))
        body_arg = (bt, reqd)
    qtype = emit_query(cls, mname, query, f'op:{m.upper()} {p}:query') if query else None
    if not emit:
        continue
    # a header the spec added after release: the shipped method keeps its parameter list and an overload takes the key
    legacy_idem = bool(idem and m != 'get' and f'{cls}.{mname}' in PINS.get('no_idem', []))
    if legacy_idem:
        USED_PINS.add(('no_idem', f'{cls}.{mname}'))
        idem = False
    sig = list(args)
    if body_arg and body_arg[1]:
        sig.append(f'{body_arg[0]} body')
    if body_arg and not body_arg[1]:
        sig.append(f'{body_arg[0]}? body = null')
    legacy_q = bool(qtype and f'{cls}.{mname}' in PINS.get('no_query', []))  # a query added after release
    if qtype and not legacy_q:
        sig.append(f'{qtype}? query = null')
    if idem and m != 'get':
        sig.append('string? idempotencyKey = null')
    sig.append('CancellationToken cancellationToken = default')
    verb = {'get': 'Get', 'post': 'Post', 'put': 'Put', 'patch': 'Patch', 'delete': 'Delete'}[m]
    call_args = [f'HttpVerb.{verb}', f'$"{path_expr}"']
    named = []
    if body_arg:
        named.append('body: body')
    if qtype and not legacy_q:
        named.append('query: query?.ToQuery()')
    if idem and m != 'get':
        named.append('idempotencyKey: idempotencyKey')
    named.append('cancellationToken: cancellationToken')
    inner = ', '.join(call_args + named)
    summ = o.get('summary') or ''
    scope = o.get('x-required-scope')
    xdoc = f'        /// <summary>{esc(summ)}</summary>\n'
    if scope:
        xdoc += f'        /// <remarks>{m.upper()} {esc(p)}. Scope: <c>{esc(scope)}</c>.</remarks>\n'
    else:
        xdoc += f'        /// <remarks>{m.upper()} {esc(p)}.</remarks>\n'
    if empty:
        src = xdoc + f'        public {ret} {mname}({", ".join(sig)}) =>\n            ReqVoid({inner.replace("cancellationToken: cancellationToken", "cancellationToken: cancellationToken")});\n'
    else:
        src = xdoc + f'        public {ret} {mname}({", ".join(sig)}) =>\n            Req<{rtype}>({inner});\n'
    r['methods'].append(src)
    if legacy_q or legacy_idem:  # required extras (an optional one would make `M(x)` ambiguous with the shipped overload)
        def _ov(with_idem):
            sg = [x.replace(' = null', '') for x in sig[:-1]]
            nm = list(named[:-1])
            if legacy_q:
                sg.append(f'{qtype}? query'); nm.append('query: query?.ToQuery()')
            if with_idem:
                sg.append('string idempotencyKey'); nm.append('idempotencyKey: idempotencyKey')
            sg.append(sig[-1]); nm.append(named[-1])
            ik = ', '.join(call_args + nm)
            body_ = f'ReqVoid({ik})' if empty else f'Req<{rtype}>({ik})'
            r['methods'].append(xdoc + f'        public {ret} {mname}({", ".join(sg)}) =>\n            {body_};\n')
        if legacy_q: _ov(False)
        if legacy_idem: _ov(True)
    if page_item and qtype and any(q['name'] in ('starting_after',) for q in query) and any(q['name'] == 'limit' for q in query):
        pname_ = mname[:-5] + 'AllAsync'
        sig2 = [a for a in args]
        if body_arg and body_arg[1]:
            sig2.append(f'{body_arg[0]} body')
        sig2 += [f'{qtype}? query = null', 'CancellationToken cancellationToken = default']
        call = ', '.join([a.split(' ')[-1] for a in args] + (['body'] if body_arg and body_arg[1] else []) + ['_q with { StartingAfter = _c }', 'cancellationToken: _ct'])
        pre = ', '.join([a.split(' ')[-1] for a in args] + (['body'] if body_arg and body_arg[1] else []))
        src2 = (f'        /// <summary>Every item across every page of <c>{mname}</c>, following <c>next_cursor</c>.</summary>\n'
                f'        public IAsyncEnumerable<{page_item}> {pname_}({", ".join(sig2)}) =>\n'
                f'            AtlasPagination.PaginateAsync<{page_item}>(async (_c, _ct) =>\n'
                f'            {{\n'
                f'                var _q = query ?? new {qtype}();\n'
                f'                var _page = await {mname}({call}).ConfigureAwait(false);\n'
                f'                return new CursorPage<{page_item}> {{ Data = _page.Data ?? new List<{page_item}>(), HasMore = (bool?)_page.HasMore ?? false, NextCursor = _page.NextCursor }};\n'
                f'            }}, query?.StartingAfter, cancellationToken);\n')
        r['methods'].append(src2)
    op_lines.append(f'{m.upper()} {p}')

# ── write files ──────────────────────────────────────────────────────────────
HEADER = ('// <auto-generated>\n// Generated by scripts/gen_catalog.py from the OpenAPI snapshot. Do not edit by hand.\n// </auto-generated>\n'
          '#nullable enable\n')
os.makedirs(GEN_DIR, exist_ok=True)

models_src = (HEADER + 'using System.Collections.Generic;\nusing System.Text.Json.Serialization;\n\nnamespace Atlas.Generated\n{\n'
              + '\n'.join(MODELS) + '}\n')
open(os.path.join(GEN_DIR, 'CatalogModels.cs'), 'w').write(models_src)

cat = [HEADER + 'using System;\nusing System.Collections.Generic;\nusing System.Runtime.CompilerServices;\nusing System.Threading;\nusing System.Threading.Tasks;\nusing Atlas.Resources;\n\nnamespace Atlas.Generated\n{\n']
acc = []
for ns in sorted(resources):
    r = resources[ns]
    cat.append(f'    /// <summary>The <c>{ns}</c> operations.</summary>\n    public sealed class {r["cls"]} : ResourceBase\n    {{\n'
               f'        internal {r["cls"]}(AtlasTransport transport) : base(transport) {{ }}\n\n' + '\n'.join(r['methods']) + '    }\n\n')
    t = '_platformTransport' if r['platform'] else '_transport'
    acc.append(f'        private {r["cls"]}? _{camel(ns).strip("@")};\n'
               f'        /// <summary>The <c>{ns}</c> operations.</summary>\n'
               f'        public {r["cls"]} {ns} => _{camel(ns).strip("@")} ??= new {r["cls"]}({"_platform" if r["platform"] else "_secret"});\n')
cat.append('''    /// <summary>
    /// The generated long tail of the Backend and Platform APIs, one property per
    /// resource namespace: <c>client.Catalog.AppCatalog.ListCustomPresetsAsync()</c>.
    /// </summary>
    public sealed class AtlasCatalog : ResourceBase
    {
        private readonly AtlasTransport _secret;
        private readonly AtlasTransport _platform;

        internal AtlasCatalog(AtlasTransport secret, AtlasTransport platform) : base(secret)
        {
            _secret = secret;
            _platform = platform;
        }

''' + '\n'.join(acc) + '    }\n}\n')
open(os.path.join(GEN_DIR, 'Catalog.cs'), 'w').write(''.join(cat))

open(os.path.join(SRC, 'AtlasClient.Catalog.cs'), 'w').write(
    'using Atlas.Generated;\n\nnamespace Atlas\n{\n    public sealed partial class AtlasClient\n    {\n'
    '        private AtlasCatalog? _catalog;\n\n'
    '        /// <summary>The generated long tail of the Backend and Platform APIs, grouped by resource namespace.</summary>\n'
    '        public AtlasCatalog Catalog => _catalog ??= new AtlasCatalog(_transport, _platformTransport);\n    }\n}\n')

# api-operations.txt: keep what is there, append the generated operations.
MARK = '# --- generated by scripts/gen_catalog.py: do not edit below ---'
cur = open(OPS_TXT).read().split('\n') if os.path.exists(OPS_TXT) else []
keep = [l for l in (cur[:cur.index(MARK)] if MARK in cur else cur) if l.strip()]
have = set(keep)
keep.append(MARK)
keep += [l for l in sorted(op_lines) if l not in have]
open(OPS_TXT, 'w').write('\n'.join(keep) + '\n')

# pins
for kind in ('methods', 'types'):
    for k in PINS[kind]:
        if (kind, k) not in USED_PINS and not PRUNE:
            sys.exit(f'stale pin {kind}: {k} (the spec no longer reaches it); remove it from catalog_pins.json')
if FREEZE:
    live = lambda kind: {k: v for k, v in PINS[kind].items() if (kind, k) in USED_PINS or not PRUNE}
    merged_pins = {'generate_anyway': PINS['generate_anyway'], 'no_idem': PINS.get('no_idem', []), 'no_query': PINS.get('no_query', []),
                   'methods': {**live('methods'), **PINS_OUT['methods']},
                   'types': {**live('types'), **PINS_OUT['types']}}
    json.dump(merged_pins, open(PINS_FILE, 'w'), indent=1, sort_keys=True)
    open(PINS_FILE, 'a').write('\n')
print(f'{len(op_lines)} operations, {len(resources)} namespaces, {len(MODELS)} records')
