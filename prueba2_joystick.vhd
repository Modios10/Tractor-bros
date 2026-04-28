library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity prueba2_joystick is
    Port (  
        boton        : in  std_logic;
        boton2       : in  std_logic;
        clk          : in  std_logic; 
        
        uart_tx      : out std_logic;
        uart_rx      : in  std_logic;

        led_azul     : out std_logic;
        led_rojo     : out std_logic;

        -- Segmentos (8 pines)
        dB, dF, dA, dG, dC, dDP, dD, dE : OUT std_logic;
        
        -- Los 4 pines que actúan como "Ground" (Multiplexación)
        dig1, dig2, dig3, dig4          : OUT std_logic
    );
end prueba2_joystick;

architecture rtl of prueba2_joystick is

    component adc0 is
        port (
            CLOCK : in  std_logic := 'X'; 
            RESET : in  std_logic := 'X'; 
            CH0   : out std_logic_vector(11 downto 0);
            CH1   : out std_logic_vector(11 downto 0);
            CH2   : out std_logic_vector(11 downto 0);
            CH3   : out std_logic_vector(11 downto 0);
            CH4   : out std_logic_vector(11 downto 0);
            CH5   : out std_logic_vector(11 downto 0);
            CH6   : out std_logic_vector(11 downto 0);
            CH7   : out std_logic_vector(11 downto 0)
        );
    end component adc0;

    signal btn1_clean, btn2_clean : std_logic;
    signal send_flag, busy        : std_logic := '0';
    signal data_to_send           : std_logic_vector(7 downto 0);
    signal timer_pulse            : std_logic;
    signal raw_x, raw_y           : std_logic_vector(11 downto 0);
    signal x_val_stored, y_val_stored, buttons_stored : std_logic_vector(7 downto 0);

    signal rx_data    : std_logic_vector(7 downto 0);
    signal rx_ready   : std_logic;
    signal byte_cnt   : integer range 0 to 3 := 0;
    
    signal reg0, reg1 : unsigned(3 downto 0) := (others => '0');

    signal refresh_cnt       : integer range 0 to 50_000 := 0; 
    signal digit_sel         : unsigned(1 downto 0) := "00";
    signal current_digit_val : unsigned(3 downto 0);
    signal segments          : std_logic_vector(6 downto 0);

    type joy_state_type is (IDLE, SEND_HEADER, SEND_X, SEND_Y, SEND_BUTTONS);
    signal joy_state : joy_state_type := IDLE;

begin

    u0 : component adc0
        port map (
            CLOCK => clk,
            RESET => '0', 
            CH0   => raw_x,
            CH1   => raw_y,
            CH2   => open,
            CH3   => open,
            CH4   => open,
            CH5   => open,
            CH6   => open,
            CH7   => open
        );

    DB1: entity work.debounce port map(clk => clk, sig_in => boton, sig_stable => btn1_clean);
    DB2: entity work.debounce port map(clk => clk, sig_in => boton2, sig_stable => btn2_clean);
    TIMER: entity work.timer_1sec generic map(CLK_FREQ => 1_000_000) port map(clk => clk, pulse => timer_pulse);
    
    TX_UNIT: entity work.uart_tx port map(clk => clk, start => send_flag, data_in => data_to_send, tx_line => uart_tx, busy => busy);
    RX_UNIT: entity work.uart_rx port map(clk => clk, rx_line => uart_rx, data_out => rx_data, new_data_tick => rx_ready);

    -- 1. PROCESO DE RECEPCIÓN
    process(clk)
    begin
        if rising_edge(clk) then
            if rx_ready = '1' then
                if rx_data = x"0A" or rx_data = x"0D" then
                    byte_cnt <= 0;
                elsif rx_data >= x"30" and rx_data <= x"39" then
                    if byte_cnt = 0 then
                        reg1 <= unsigned(rx_data(3 downto 0));
                        byte_cnt <= 1;
                    elsif byte_cnt = 1 then
                        reg0 <= unsigned(rx_data(3 downto 0));
                        byte_cnt <= 2;
                    end if;
                end if;
            end if;
        end if;
    end process;

    -- 2. REFRESH DE MULTIPLEXACIÓN (1 kHz)
    process(clk)
    begin
        if rising_edge(clk) then
            if refresh_cnt = 50_000 then
                refresh_cnt <= 0;
                digit_sel <= digit_sel + 1;
            else
                refresh_cnt <= refresh_cnt + 1;
            end if;
        end if;
    end process;

    -- 3. MUX: LA FPGA DA EL "GROUND" (0) AL DÍGITO ACTIVO
    process(digit_sel, reg0, reg1)
    begin
        case digit_sel is
            when "00" => 
                current_digit_val <= reg0;
                dig1 <= '1'; dig2 <= '1'; dig3 <= '1'; dig4 <= '0'; 
            when "01" => 
                current_digit_val <= reg1;
                dig1 <= '1'; dig2 <= '1'; dig3 <= '0'; dig4 <= '1'; 
            when "10" => 
                current_digit_val <= "1111"; -- 1111 es nuestro código de "apagado"
                dig1 <= '1'; dig2 <= '0'; dig3 <= '1'; dig4 <= '1'; 
            when "11" => 
                current_digit_val <= "1111";
                dig1 <= '0'; dig2 <= '1'; dig3 <= '1'; dig4 <= '1'; 
            when others => 
                current_digit_val <= "1111";
        end case;
    end process;

    -- 4. DECODIFICADOR USANDO BINARIO ESTRICTO (Para evitar el error del x"0")
    process(current_digit_val)
    begin
        case current_digit_val is
            when "0000" => segments <= "1111110"; -- abcdefg (0)
            when "0001" => segments <= "0110000"; -- 1
            when "0010" => segments <= "1101101"; -- 2
            when "0011" => segments <= "1111001"; -- 3
            when "0100" => segments <= "0110011"; -- 4
            when "0101" => segments <= "1011011"; -- 5
            when "0110" => segments <= "1011111"; -- 6
            when "0111" => segments <= "1110000"; -- 7
            when "1000" => segments <= "1111111"; -- 8
            when "1001" => segments <= "1111011"; -- 9
            when others => segments <= "0000000"; -- Todo apagado
        end case;
    end process;

    -- 5. MAPEO A PINES FÍSICOS
    dA <= segments(6); dB <= segments(5); dC <= segments(4); dD <= segments(3);
    dE <= segments(2); dF <= segments(1); dG <= segments(0); dDP <= '0';

    -- Agrupamos los botones de forma directa para evitar errores de concatenación "&"
    buttons_stored(7 downto 2) <= "000000";
    buttons_stored(1) <= not btn2_clean;
    buttons_stored(0) <= not btn1_clean;

    -- 6. TRANSMISIÓN DEL JOYSTICK
    process(clk)
    begin
        if rising_edge(clk) then
            led_azul <= not btn1_clean;
            led_rojo <= not btn2_clean;
            send_flag <= '0'; 

            if busy = '0' and send_flag = '0' then
                case joy_state is
                    when IDLE =>
                        if timer_pulse = '1' then
                            x_val_stored <= raw_x(11 downto 4);
                            y_val_stored <= raw_y(11 downto 4);
                            joy_state <= SEND_HEADER;
                        end if;
                    when SEND_HEADER =>
                        data_to_send <= x"FF"; send_flag <= '1'; joy_state <= SEND_X;
                    when SEND_X =>
                        data_to_send <= x_val_stored; send_flag <= '1'; joy_state <= SEND_Y;
                    when SEND_Y =>
                        data_to_send <= y_val_stored; send_flag <= '1'; joy_state <= SEND_BUTTONS;
                    when SEND_BUTTONS =>
                        data_to_send <= buttons_stored; send_flag <= '1'; joy_state <= IDLE;
                    when others => joy_state <= IDLE;
                end case;
            end if;
        end if;
    end process;

end rtl;
