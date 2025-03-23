
mod gl_wrap;

use glfw::{ Context, PWindow, Glfw, GlfwReceiver, WindowEvent, Key, Action };

pub struct Application {
    test_shader_program: gl_wrap::shader::Program,


    event_reciever: GlfwReceiver<(f64, WindowEvent)>,
    main_window: PWindow,
    // WARNING: Glfw should be dropped LAST (for safety when destroying any OpenGL objects)
    glfw: Glfw,
}

impl Application {
    pub fn new(width: u32, height: u32) -> Self {
        // init GLFW
        let mut glfw = glfw::init(glfw::fail_on_errors).unwrap();
        // create GLFW window
        let (mut main_window, event_reciever) = glfw.create_window(width, height, "Hello this is window", glfw::WindowMode::Windowed)
            .expect("Failed to create GLFW window.");
        main_window.set_key_polling(true);
        main_window.make_current();

        gl::load_with(|s| main_window.get_proc_address(s) as *const _);

        // other init
        let fragment_src = include_str!("../shaders/test.frag");
        let vertex_src = include_str!("../shaders/test.vert");

        let fragment = gl_wrap::shader::Shader::from_str(fragment_src, gl::FRAGMENT_SHADER);
        let vertex = gl_wrap::shader::Shader::from_str(vertex_src, gl::VERTEX_SHADER);

        let program = gl_wrap::shader::Program::from_shaders([&fragment, &vertex].into_iter());

        Self {
            glfw,
            main_window,
            event_reciever,
            test_shader_program: program,
        }
    }
    pub fn run(&mut self) {
        while !self.main_window.should_close() {
            // handle window input
            self.handle_events();

            // update program state (run simulation)
            self.update();

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
    fn update(&mut self) {
        // test some vector math
        let x = linalg::Vector::from([1.0f32, 0.0f32]);
        let rot90ccw = linalg::transform_2x2::rotate(0.5*std::f32::consts::PI);
        println!("{:?}", rot90ccw*x);
    }
    fn draw(&mut self) {
        self.test_shader_program.bind();

        unsafe { gl::DrawArrays(gl::TRIANGLES, 0, 3) };
    }
}

