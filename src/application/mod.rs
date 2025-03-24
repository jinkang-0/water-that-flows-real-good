
mod gl_wrap;

use glfw::{ Context, PWindow, Glfw, GlfwReceiver, WindowEvent, Key, Action };

use gl_wrap::shader;

/// Simple timer structure for keeping track of frame timing
struct FrameTimer {
    t_start: f64,
    t_end: f64,
}
impl FrameTimer {
    fn new() -> Self { Self { t_start: 0.0, t_end: 0.0, } }
    /// Mark the start of the frame
    /// returns the time for the previous frame
    fn start_frame(&mut self, current_time: f64) -> f64 {
        self.t_end = self.t_start;
        self.t_start = current_time;
        self.t_start - self.t_end
    }
}

pub struct Application {
    test_shader_program: shader::Program,
    test_vao: gl_wrap::VAO,


    frame_timer: FrameTimer,
    // Window Objects
    event_reciever: GlfwReceiver<(f64, WindowEvent)>,
    main_window: PWindow,
    // WARNING: Glfw should be dropped LAST (for safety when destroying any OpenGL objects)
    glfw: Glfw
}

impl Application {
    pub fn new(width: u32, height: u32) -> Self {
        // init GLFW
        let mut glfw = glfw::init(glfw::fail_on_errors).unwrap();
        // create GLFW window
        glfw.window_hint(glfw::WindowHint::ContextVersion(3, 3));
        glfw.window_hint(glfw::WindowHint::OpenGlProfile(glfw::OpenGlProfileHint::Core));
        let (mut main_window, event_reciever) = glfw.create_window(width, height, "Hello this is window", glfw::WindowMode::Windowed)
            .expect("Failed to create GLFW window.");
        main_window.set_key_polling(true);
        main_window.make_current();

        gl::load_with(|s| main_window.get_proc_address(s) as *const _);

        // print OpenGL version
        let gl_version_cstr = unsafe { std::ffi::CStr::from_ptr(gl::GetString(gl::VERSION) as *const i8) };
        println!("Using OpenGL version: {:?}", gl_version_cstr);

        // other init
        let fragment_src = include_str!("../shaders/test.frag");
        let vertex_src = include_str!("../shaders/test.vert");

        let fragment = shader::Shader::from_str(fragment_src, gl::FRAGMENT_SHADER);
        let vertex = shader::Shader::from_str(vertex_src, gl::VERTEX_SHADER);

        let program = shader::Program::from_shaders([&fragment, &vertex].into_iter());

        // The vertex array object is not really used for anything yet
        // It is here because drawing without creating one is deprecated (the default VAO is depricated)
        let vao = gl_wrap::VAO::new();


        // done initializing, set time to zero
        glfw.set_time(0.0);
        Self {
            glfw,
            main_window,
            event_reciever,
            frame_timer: FrameTimer::new(),

            test_vao: vao,
            test_shader_program: program,
        }
    }
    pub fn run(&mut self) {
        while !self.main_window.should_close() {
            let t = self.glfw.get_time();
            let dt = self.frame_timer.start_frame(t);

            // handle window input
            self.handle_events();

            // update program state (run simulation)
            self.update(t, dt);

            // update OpenGL viewport and clear screen
            let window_size = self.main_window.get_framebuffer_size();
            unsafe { gl::Viewport(0, 0, window_size.0, window_size.1) };

            unsafe { gl::ClearColor(0.02, 0.02, 0.02, 1.0) };
            unsafe { gl::Clear(gl::COLOR_BUFFER_BIT) };

            // draw
            self.draw();

            // present
            self.main_window.swap_buffers();
        }
    }

    fn handle_events(&mut self) {
        // poll for new events
        self.glfw.poll_events();
        // loop through new events
        for (_time, event) in glfw::flush_messages(&self.event_reciever) {
            match event {
                glfw::WindowEvent::Key(Key::Escape, _, Action::Press, _) => self.main_window.set_should_close(true),
                _ => {}
            }
        }
    }
    fn update(&mut self, t: f64, dt: f64) {
        // test some vector math
        let x = linalg::Vector::from([1.0f32, 0.0f32]);
        let rot90ccw = linalg::transform_2x2::rotate(0.5*std::f32::consts::PI);
        println!("{:?}", rot90ccw*x);
        // print some frame timing info
        println!("Time: {}, Delta Time: {}, Framerate: {}", t, dt, 1.0 / dt);
    }
    fn draw(&mut self) {
        self.test_vao.bind();
        self.test_shader_program.bind();

        unsafe { gl::DrawArrays(gl::TRIANGLES, 0, 3) };
    }
}

