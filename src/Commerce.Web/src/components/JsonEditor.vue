<template>
  <div
    class="json-editor-wrapper rounded-lg overflow-hidden focus-within:ring-2 focus-within:ring-primary-500 focus-within:ring-offset-0 transition-shadow"
    :class="[
      'border',
      invalid ? 'border-red-500 dark:border-red-500 ring-1 ring-red-500/20' : 'border-gray-300 dark:border-gray-600'
    ]"
  >
    <Codemirror
      :value="modelValue"
      @update:value="$emit('update:modelValue', $event)"
      :options="cmOptions"
      :border="false"
      :placeholder="placeholder"
      :height="height"
      @blur="$emit('blur')"
      @focus="$emit('focus')"
    />
  </div>
</template>

<script setup>
import Codemirror from 'codemirror-editor-vue3'
import 'codemirror/mode/javascript/javascript.js'
import 'codemirror/addon/display/placeholder.js'
import 'codemirror/addon/edit/matchbrackets.js'
import 'codemirror/addon/edit/closebrackets.js'

const props = defineProps({
  modelValue: { type: String, default: '' },
  placeholder: { type: String, default: '' },
  height: { type: [String, Number], default: '320px' },
  invalid: { type: Boolean, default: false },
})

defineEmits(['update:modelValue', 'blur', 'focus'])

const cmOptions = {
  mode: 'application/json',
  theme: 'default',
  lineNumbers: true,
  indentUnit: 2,
  tabSize: 2,
  indentWithTabs: false,
  lineWrapping: true,
  matchBrackets: true,
  autoCloseBrackets: true,
}
</script>

<style scoped>
/* Platform-aligned editor styling */
.json-editor-wrapper :deep(.CodeMirror) {
  font-size: 0.875rem;
  font-family: ui-monospace, SFMono-Regular, 'SF Mono', Menlo, Consolas, monospace;
  background-color: #ffffff !important;
  color: #111827 !important;
}
.json-editor-wrapper :deep(.CodeMirror-gutters) {
  background-color: #f9fafb !important;
  border-right: 1px solid #e5e7eb !important;
  color: #9ca3af !important;
}
.json-editor-wrapper :deep(.CodeMirror-cursor) {
  border-left-color: #0ea5e9 !important;
}
.json-editor-wrapper :deep(.CodeMirror-selected) {
  background: rgba(14, 165, 233, 0.2) !important;
}
.json-editor-wrapper :deep(.CodeMirror-line::selection),
.json-editor-wrapper :deep(.CodeMirror-line > span::selection) {
  background: rgba(14, 165, 233, 0.2) !important;
}
.json-editor-wrapper :deep(.CodeMirror-activeline-background) {
  background: rgba(14, 165, 233, 0.05) !important;
}
/* JSON syntax colors - light mode */
.json-editor-wrapper :deep(.cm-key) { color: #0369a1 !important; }
.json-editor-wrapper :deep(.cm-string) { color: #047857 !important; }
.json-editor-wrapper :deep(.cm-number) { color: #b45309 !important; }
.json-editor-wrapper :deep(.cm-atom) { color: #7c3aed !important; }
.json-editor-wrapper :deep(.cm-property) { color: #0369a1 !important; }
.json-editor-wrapper :deep(.CodeMirror-placeholder) { color: #9ca3af !important; }
</style>

<style>
/* Dark mode - must be global for :root.dark */
.dark .json-editor-wrapper .CodeMirror {
  background-color: #111827 !important;
  color: #f3f4f6 !important;
}
.dark .json-editor-wrapper .CodeMirror-gutters {
  background-color: #1f2937 !important;
  border-right-color: #374151 !important;
  color: #6b7280 !important;
}
.dark .json-editor-wrapper .CodeMirror-cursor {
  border-left-color: #38bdf8 !important;
}
.dark .json-editor-wrapper .CodeMirror-selected {
  background: rgba(56, 189, 248, 0.2) !important;
}
.dark .json-editor-wrapper .CodeMirror-line::selection,
.dark .json-editor-wrapper .CodeMirror-line > span::selection {
  background: rgba(56, 189, 248, 0.2) !important;
}
.dark .json-editor-wrapper .CodeMirror-activeline-background {
  background: rgba(56, 189, 248, 0.08) !important;
}
.dark .json-editor-wrapper .cm-key { color: #7dd3fc !important; }
.dark .json-editor-wrapper .cm-string { color: #6ee7b7 !important; }
.dark .json-editor-wrapper .cm-number { color: #fcd34d !important; }
.dark .json-editor-wrapper .cm-atom { color: #a78bfa !important; }
.dark .json-editor-wrapper .cm-property { color: #7dd3fc !important; }
.dark .json-editor-wrapper .CodeMirror-placeholder { color: #6b7280 !important; }
</style>
