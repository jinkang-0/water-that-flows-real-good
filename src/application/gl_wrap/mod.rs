
pub mod utils;
#[allow(dead_code)]
pub mod shader;

use gl::types::{GLuint, GLenum};

#[derive(Debug)]
#[repr(u32)]
pub enum OpenGLError {
    InvalidEnum = gl::INVALID_ENUM,
    InvalidValue = gl::INVALID_VALUE,
    InvalidOperation = gl::INVALID_OPERATION,
    StackOverflow = gl::STACK_OVERFLOW,
    StackUnderflow = gl::STACK_UNDERFLOW,
    OutOfMemory = gl::OUT_OF_MEMORY,
    InvalidFramebufferOperation = gl::INVALID_FRAMEBUFFER_OPERATION,
    ContextLost = gl::CONTEXT_LOST,

    UnknownError = gl::NO_ERROR,
}
fn gl_get_error() -> Result<(), OpenGLError> {
    match unsafe { gl::GetError() } {
        gl::NO_ERROR => Ok(()),
        gl::INVALID_ENUM => Err(OpenGLError::InvalidEnum),
        gl::INVALID_VALUE => Err(OpenGLError::InvalidValue),
        gl::INVALID_OPERATION => Err(OpenGLError::InvalidOperation),
        gl::STACK_OVERFLOW => Err(OpenGLError::StackOverflow),
        gl::STACK_UNDERFLOW => Err(OpenGLError::StackUnderflow),
        gl::OUT_OF_MEMORY => Err(OpenGLError::OutOfMemory),
        gl::INVALID_FRAMEBUFFER_OPERATION => Err(OpenGLError::InvalidFramebufferOperation),
        gl::CONTEXT_LOST => Err(OpenGLError::ContextLost),
        _ => Err(OpenGLError::UnknownError)
    }
}
pub fn gl_check_errors(line: u32, file: &str) {
    // print every error
    while let Err(e) = gl_get_error() {
        eprintln!("\x1b[1;31mOpenGL Error:\x1b[0m \x1b[1m{:?}\x1b[0m ({}:{})", e, file, line);
    }
}

/// (semi-safe) Wraper for OpenGL vertex array objects
pub struct VAO {
    handle: GLuint
}

#[allow(dead_code)]
impl VAO {
    pub fn new() -> Self {
        let mut handles = [0];
        let handles_ptr = handles.as_mut_ptr();
        // could also just do `DeleteVertexArrays(1, &raw mut handle)`, but this more "correct"
        unsafe { gl::GenVertexArrays(handles.len().try_into().unwrap(), handles_ptr) };
        Self { handle: handles[0] }
    }
    pub fn bind(&self) {
        unsafe { gl::BindVertexArray(self.handle) };
    }
    pub fn handle(&self) -> GLuint {
        self.handle
    }
}
impl Drop for VAO {
    fn drop(&mut self) {
        let handles = [self.handle];
        let handles_ptr = handles.as_ptr();
        // could also just do `DeleteVertexArrays(1, &raw mut handle)`, but this more "correct"
        unsafe { gl::DeleteVertexArrays(handles.len().try_into().unwrap(), handles_ptr) };
    }
}

/// (semi-safe) Wraper for OpenGL vertex buffer objects
pub struct VBO {
    handle: GLuint
}
#[allow(dead_code)]
impl VBO {
    pub fn new() -> Self {
        let mut handles = [0];
        let handles_ptr = handles.as_mut_ptr();
        // could also just do `GenBuffers(1, &raw mut handle)`, but this more "correct"
        unsafe { gl::GenBuffers(handles.len().try_into().unwrap(), handles_ptr) };
        Self { handle: handles[0] }
    }
    pub fn bind(&self, target: GLenum) {
        unsafe { gl::BindBuffer(target, self.handle) };
    }
    pub fn upload_data<T>(&self, data: &[T], usage: u32) {
        let total_size: isize = (std::mem::size_of::<T>() * data.len()).try_into().unwrap();
        unsafe { gl::BufferData(gl::ARRAY_BUFFER, total_size, data.as_ptr() as *const std::ffi::c_void, usage) };
    }
    pub fn handle(&self) -> GLuint {
        self.handle
    }
}

impl Drop for VBO {
    fn drop(&mut self) {
        let handles = [self.handle];
        let handles_ptr = handles.as_ptr();
        // could also just do `DeleteBuffers(1, &raw mut handle)`, but this more "correct"
        unsafe { gl::DeleteBuffers(handles.len().try_into().unwrap(), handles_ptr) };
    }
}

