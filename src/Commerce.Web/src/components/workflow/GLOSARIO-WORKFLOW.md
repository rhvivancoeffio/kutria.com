# Glosario: editor de workflow

Lenguaje común para describir el editor (nodos, puertos, conexiones) y su equivalente en código.

---

## 1. Nodos (nodes)

| Tú dices | En el código / Vue Flow |
|----------|-------------------------|
| **Nodo** | **Node** (objeto con `id`, `type`, `position`, `data`). En el canvas es un “paso” del flujo. |
| **Nodo tipo Action** | Node con `type: 'action'`. Componente: `ActionNode.vue`. En el modelo: `step.type === 'action'`. |
| **Nodo tipo Conditional** | Node con `type: 'conditional'`. Componente: `ConditionalNode.vue`. En el modelo: `step.type === 'conditional'`. |
| **Nodo Inicio** | Node con `id: 'start'`. Componente: `StartNode.vue`. Solo hay uno; no es un “step” del JSON. |

Resumen: un **nodo** = un elemento visual del grafo (cuadro con nombre, icono, etc.). Los tipos que usamos son **Action**, **Conditional** e **Inicio**.

---

## 2. Puertos (ports) = Handle + connector

| Tú dices | En el código |
|----------|--------------|
| **Port** | En Vue Flow se llama **Handle**: es el punto donde se puede iniciar o terminar una conexión. |
| **Port de entrada / input** | **Handle** con `type="target"`. Solo recibe conexiones (no se arrastra desde ahí). |
| **Port de salida / output** | **Handle** con `type="source"`. Desde aquí se arrastra la conexión. |
| **Línea** (la rayita que sale del nodo hacia el port) | **Connector** (en el HTML: `action-node__connector` + `action-node__connector-line`). Es decorativa; el punto “conectable” es el Handle. |
| **Cajita [ ]** (donde se suelta o se conecta el cable) | El **Handle** en sí: Vue Flow lo pinta como pequeño cuadrado/círculo; es el único elemento con el que se crean/rompen conexiones. |

Resumen: **port** en tu lenguaje = en código es el **Handle** (la “cajita”). La **línea** es el **connector** (solo visual). Juntos (línea + cajita) forman lo que tú llamas “un port”.

---

## 3. Por tipo de nodo

### Nodo **Action**
- **1 port input:** 1 Handle `type="target"` con `id="target"` (izquierda).  
  En tu lenguaje: “port de entrada del Action”.
- **N ports output:** N Handles `type="source"` con ids `out-1`, `out-2`, `out-3` (por defecto; `step.outputHandles`). Cada port una conexión; modelo `step.outputs`.  
  En tu lenguaje: “port de salida del Action”.  
  Puede tener **varias conexiones** saliendo del mismo port.

### Nodo **Conditional**
- **1 port input:** 1 Handle `type="target"` con `id="target"` (izquierda).  
  “Port de entrada del Conditional”.
- **2 ports output:**  
  - Handle `type="source"` con `id="success"` → **True**.  
  - Handle `type="source"` con `id="failure"` → **False**.  
  Cada uno tiene **una sola conexión** (`connectable="single"`).

### Nodo **Inicio**
- Solo **1 port output:** 1 Handle `type="source"` (derecha).  
  Puede tener **varias conexiones** a distintos nodos.

---

## 4. Conexiones (edges)

| Tú dices | En el código |
|----------|--------------|
| **Conexión** / **línea entre dos nodos** | **Edge** (objeto con `id`, `source`, `target` y opcionalmente `sourceHandle`, `targetHandle`). |
| **De qué port sale** | `source` = id del nodo, `sourceHandle` = id del Handle (ej. `'success'`, `'failure'`; si no hay, un solo output). |
| **A qué port llega** | `target` = id del nodo, `targetHandle` = id del Handle (en nuestros nodos suele ser `'target'`). |

Resumen: **conexión** = **edge** en código; cada edge une un **port de salida** (Handle source) con un **port de entrada** (Handle target).

---

## 5. Nombres que usar de aquí en adelante

Para que podamos entendernos en requerimientos y en código:

- **Nodo** = node (Action, Conditional o Inicio).
- **Port** = Handle (la “cajita” conectable); la “línea” que va del cuerpo del nodo a la cajita es el **connector** (solo visual).
- **Port input** = Handle `type="target"`.
- **Port output** = Handle `type="source"` (en Conditional: **True** = `id="success"`, **False** = `id="failure"`).
- **Conexión** = edge (línea entre dos ports).

Si en un requerimiento dices “el port de salida del Action” o “los dos ports True y False del Conditional”, en código serán los Handles `source` con los ids correspondientes.
