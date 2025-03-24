
pub mod utils;
#[allow(dead_code)]
pub mod shader;

use gl::types::GLuint;

pub struct VAO {
    handle: GLuint
}

#[allow(dead_code)]
/// (semi-safe) Wraper for OpenGL vertex array objects
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

