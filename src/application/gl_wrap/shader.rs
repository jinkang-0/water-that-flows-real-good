
use super::utils::ScopedClosure;
use gl::types::{GLuint, GLenum};

/// (semi-safe) Wraper for OpenGL shader objects
pub struct Shader {
    handle: GLuint,
}
impl Shader {
    pub fn try_from_str(src: &str, ty: GLenum) -> Result<Self, String> {
        // create handle for shader
        let handle = unsafe { gl::CreateShader(ty) };
        // scoped closure guarantees handle will be destroyed (even if a panic occurs)
        let handle_scoped = ScopedClosure::new(|| unsafe { gl::DeleteShader(handle); });

        // convert shader source to correct type/format
        let src = std::ffi::CString::new(src).expect("shader source could not be converted to CString");
        let srcs = [src.as_ptr()];

        // upload shader source and compile
        unsafe { gl::ShaderSource(handle, srcs.len().try_into().unwrap(), srcs.as_ptr(), std::ptr::null()) };
        unsafe { gl::CompileShader(handle) };

        // check if compilation succeeded
        let mut success: i32 = 0;
        unsafe { gl::GetShaderiv(handle, gl::COMPILE_STATUS, &raw mut success) };
        if success == 0 {
            // compilation failed
            let info_log = {
                // get info log length
                let mut info_log_len: i32 = 0;
                unsafe { gl::GetShaderiv(handle, gl::INFO_LOG_LENGTH, &raw mut info_log_len) };
                let info_log_len_usize: usize = info_log_len.try_into().expect("invalid OpenGL shader info log length");
                // get info log
                let mut info_log: Vec<u8> = vec![0; info_log_len_usize];
                unsafe { gl::GetShaderInfoLog(handle, info_log_len, std::ptr::null_mut(), info_log.as_mut_ptr() as *mut i8) };
                std::ffi::CString::from_vec_with_nul(info_log).expect("OpenGL shader info log could not be converted to CString")
            };
            Err(info_log.try_into().expect("OpenGL shader info log could not be converted to String"))
        } else {
            // compilation succeeded
            // forget scoped closure (transfer handle "ownership" to the Shader struct)
            handle_scoped.forget();
            Ok(Self { handle })
        }
    }
    pub fn from_str(src: &str, ty: GLenum) -> Self {
        Self::try_from_str(src, ty).expect("OpenGL shader compilation error(s)")
    }
    pub fn handle(&self) -> GLuint {
        self.handle
    }
}
impl Drop for Shader {
    fn drop(&mut self) {
        unsafe { gl::DeleteShader(self.handle) };
    }
}

/// (semi-safe) Wraper for OpenGL program objects
pub struct Program {
    handle: GLuint,
}
impl Program {
    pub fn try_from_shaders<'a>(shaders: impl Iterator<Item=&'a Shader>) -> Result<Self, String> {
        // create handle for program
        let handle = unsafe { gl::CreateProgram() };
        // scoped closure guarantees handle will be destroyed (even if a panic occurs)
        let handle_scoped = ScopedClosure::new(|| unsafe { gl::DeleteProgram(handle); });
        // add shaders to program
        for shader in shaders {
            unsafe { gl::AttachShader(handle, shader.handle) };
        }
        // link
        unsafe { gl::LinkProgram(handle) };
        // check if linking succeeded
        let mut success: i32 = 0;
        unsafe { gl::GetProgramiv(handle, gl::LINK_STATUS, &raw mut success) };
        if success == 0 {
            // linking failed
            let info_log = {
                // get info log length
                let mut info_log_len: i32 = 0;
                unsafe { gl::GetProgramiv(handle, gl::INFO_LOG_LENGTH, &raw mut info_log_len) };
                let info_log_len_usize: usize = info_log_len.try_into().expect("invalid OpenGL shader info log length");
                // get info log
                let mut info_log: Vec<u8> = vec![0; info_log_len_usize];
                unsafe { gl::GetProgramInfoLog(handle, info_log_len, std::ptr::null_mut(), info_log.as_mut_ptr() as *mut i8) };
                println!("{} {:?}", info_log_len_usize, info_log);
                std::ffi::CString::from_vec_with_nul(info_log).expect("OpenGL shader info log could not be converted to CString")
            };
            Err(info_log.try_into().expect("OpenGL shader info log could not be converted to String"))
        } else {
            // linking succeeded
            // forget scoped closure (transfer handle "ownership" to the Program struct)
            handle_scoped.forget();
            Ok(Self { handle })
        }
    }
    pub fn from_shaders<'a>(shaders: impl Iterator<Item=&'a Shader>) -> Self {
        Self::try_from_shaders(shaders).expect("OpenGL shader linking error(s)")
    }
    pub fn bind(&self) {
        unsafe { gl::UseProgram(self.handle) };
    }
    pub fn handle(&self) -> GLuint {
        self.handle
    }
}
impl Drop for Program {
    fn drop(&mut self) {
        unsafe { gl::DeleteProgram(self.handle) };
    }
}

