import { useEffect, useState } from 'react'
import { useEditor, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Link from '@tiptap/extension-link'
import Underline from '@tiptap/extension-underline'
import { useAdminDialog } from './AdminDialog'

export function RichTextEditor({ value, onChange }) {
  const dialog = useAdminDialog()
  const [, refreshSelection] = useState(0)
  const editor = useEditor({
    extensions: [
      StarterKit,
      Underline,
      Link.configure({
        openOnClick: false,
        HTMLAttributes: { target: '_blank', rel: 'noopener noreferrer' },
      }),
    ],
    content: value || '',
    onUpdate: ({ editor: current }) => onChange(current.getHTML()),
    onSelectionUpdate: () => refreshSelection(current => current + 1),
  })

  useEffect(() => {
    if (editor && value !== editor.getHTML()) {
      editor.commands.setContent(value || '', { emitUpdate: false })
    }
  }, [editor, value])

  if (!editor) return null

  const hasSelection = !editor.state.selection.empty
  const applySelectedMark = mark => {
    const chain = editor.chain().focus()
    if (mark === 'bold') chain.toggleBold()
    if (mark === 'italic') chain.toggleItalic()
    if (mark === 'underline') chain.toggleUnderline()
    if (hasSelection) {
      const end = editor.state.selection.to
      chain.setTextSelection(end).unsetMark(mark)
    }
    chain.run()
  }
  const clearSelectedFormatting = () => {
    if (!hasSelection) return
    const end = editor.state.selection.to
    editor.chain().focus().unsetAllMarks().setTextSelection(end).run()
  }
  const editLink = async () => {
    const url = await dialog.prompt({
      title: 'Edit link',
      label: 'Link URL',
      defaultValue: editor.getAttributes('link').href ?? 'https://',
      confirmLabel: 'Apply link',
      inputType: 'url',
    })
    if (url === null) return
    if (!url) editor.chain().focus().unsetLink().run()
    else editor.chain().focus().extendMarkRange('link').setLink({ href: url }).run()
  }
  const disableDoubleClickSelection = event => {
    event.preventDefault()
    const position = editor.view.posAtCoords({ left: event.clientX, top: event.clientY })?.pos
    if (typeof position === 'number') {
      editor.chain().focus().setTextSelection(position).run()
    }
  }

  return (
    <div className="rich-editor">
      <div className="rich-toolbar" onMouseDown={event => {
        if (event.target.closest('button')) event.preventDefault()
      }}>
        <button type="button" className={!hasSelection && editor.isActive('bold') ? 'active' : ''} title="Toggle bold" onClick={() => applySelectedMark('bold')}><strong>B</strong></button>
        <button type="button" className={!hasSelection && editor.isActive('italic') ? 'active' : ''} title="Toggle italic" onClick={() => applySelectedMark('italic')}><em>I</em></button>
        <button type="button" className={!hasSelection && editor.isActive('underline') ? 'active' : ''} title="Toggle underline" onClick={() => applySelectedMark('underline')}><u>U</u></button>
        <button type="button" onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}>H2</button>
        <button type="button" onClick={() => editor.chain().focus().toggleBulletList().run()}>• List</button>
        <button type="button" onClick={() => editor.chain().focus().toggleOrderedList().run()}>1. List</button>
        <button type="button" className={editor.isActive('link') ? 'active' : ''} onClick={editLink}>Link</button>
        <button type="button" onClick={() => editor.chain().focus().toggleBlockquote().run()}>Quote</button>
        <span className="rich-toolbar-end">
          <button type="button" className="rich-symbol-button" aria-label="Undo" title="Undo" disabled={!editor.can().undo()} onClick={() => editor.chain().focus().undo().run()}>↶</button>
          <button type="button" className="rich-symbol-button" aria-label="Redo" title="Redo" disabled={!editor.can().redo()} onClick={() => editor.chain().focus().redo().run()}>↷</button>
          <button type="button" className="rich-symbol-button" aria-label="Clear selected formatting" title="Clear selected formatting" disabled={!hasSelection} onClick={clearSelectedFormatting}>⌫</button>
        </span>
      </div>
      <EditorContent editor={editor} onDoubleClick={disableDoubleClickSelection} />
    </div>
  )
}
