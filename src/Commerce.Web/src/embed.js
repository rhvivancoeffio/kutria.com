import { createApp } from 'vue'
import ChatWidget from './components/chat/ChatWidget.vue'
import widgetCss from './components/chat/chat-widget.css?inline'

const script = document.currentScript
const api = script?.dataset.api || ''
const tenant = script?.dataset.tenant || 'tenant1'

const host = document.createElement('div')
host.id = 'commerce-chat-embed'
document.body.appendChild(host)

const shadow = host.attachShadow({ mode: 'open' })
const style = document.createElement('style')
style.textContent = widgetCss
shadow.appendChild(style)

const mount = document.createElement('div')
shadow.appendChild(mount)

createApp(ChatWidget, {
  apiBase: api,
  tenant,
  embedded: true
}).mount(mount)
