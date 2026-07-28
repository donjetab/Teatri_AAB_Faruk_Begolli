import { useEffect } from 'react'
import { useEditor, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import Link from '@tiptap/extension-link'
import Underline from '@tiptap/extension-underline'

export function RichTextEditor({ value, onChange }) {
  const editor = useEditor({
    extensions: [StarterKit, Underline, Link.configure({ openOnClick: false, HTMLAttributes: { target: '_blank', rel: 'noopener noreferrer' } })],
    content: value || '',
    onUpdate: ({ editor: current }) => onChange(current.getHTML()),
  })
  useEffect(() => { if (editor && value !== editor.getHTML()) editor.commands.setContent(value || '', { emitUpdate: false }) }, [editor, value])
  if (!editor) return null
  const link = () => { const url = window.prompt('Link URL', editor.getAttributes('link').href ?? 'https://'); if (url === null) return; if (!url) editor.chain().focus().unsetLink().run(); else editor.chain().focus().extendMarkRange('link').setLink({ href: url }).run() }
  return <div className="rich-editor"><div className="rich-toolbar">
    <button type="button" className={editor.isActive('bold') ? 'active' : ''} onClick={() => editor.chain().focus().toggleBold().run()}><strong>B</strong></button>
    <button type="button" className={editor.isActive('italic') ? 'active' : ''} onClick={() => editor.chain().focus().toggleItalic().run()}><em>I</em></button>
    <button type="button" className={editor.isActive('underline') ? 'active' : ''} onClick={() => editor.chain().focus().toggleUnderline().run()}><u>U</u></button>
    <button type="button" onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}>H2</button>
    <button type="button" onClick={() => editor.chain().focus().toggleBulletList().run()}>• List</button>
    <button type="button" onClick={() => editor.chain().focus().toggleOrderedList().run()}>1. List</button>
    <button type="button" className={editor.isActive('link') ? 'active' : ''} onClick={link}>Link</button>
    <button type="button" onClick={() => editor.chain().focus().toggleBlockquote().run()}>Quote</button>
    <button type="button" onClick={() => editor.chain().focus().undo().run()}>Undo</button>
    <button type="button" onClick={() => editor.chain().focus().redo().run()}>Redo</button>
    <button type="button" onClick={() => editor.chain().focus().clearNodes().unsetAllMarks().run()}>Clear</button>
  </div><EditorContent editor={editor} /></div>
}
